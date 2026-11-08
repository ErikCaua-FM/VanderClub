using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using MySqlConnector;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Services;

var bootstrap = args.Contains("--bootstrap");
var verify = args.Contains("--verify");
var builder = WebApplication.CreateBuilder(args.Where(a => a is not "--bootstrap" and not "--verify").ToArray());
SqlMapper.AddTypeHandler(new DateOnlyHandler());
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 128 * 1024);
builder.Services.AddSingleton<Banco>();
builder.Services.AddSingleton<Autenticacao>();
builder.Services.AddSingleton<Execucao>();
builder.Services.AddSingleton<ClubeService>();
builder.Services.AddSingleton<Inicializacao>();
builder.Services.AddHostedService<RotinasService>();
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = c => new BadRequestObjectResult(new { code = "INVALID_BODY", message = "Confira o formato e os campos enviados.", requestId = c.HttpContext.TraceIdentifier }));
builder.Services.AddAuthentication("Sessao").AddScheme<AuthenticationSchemeOptions, SessaoHandler>("Sessao", _ =>
{
});
builder.Services.AddAuthorization();
var origins = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
if (origins.Any(o => o == "*" || !Uri.TryCreate(o, UriKind.Absolute, out _)))
    throw new InvalidOperationException("Cors:Origens deve conter origens exatas (sem '*').");
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyMethod().WithHeaders("Content-Type", "Accept", "X-CSRF-Token", "Idempotency-Key", "Authorization").AllowCredentials()));
builder.Services.AddHttpsRedirection(o => o.HttpsPort = builder.Configuration.GetValue("HttpsPort", 7147));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c => RateLimitPartition.GetFixedWindowLimiter(c.User.Identity?.IsAuthenticated == true ? "u:" + c.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : "ip:" + c.Connection.RemoteIpAddress, _ => new FixedWindowRateLimiterOptions { PermitLimit = 240, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    o.OnRejected = async (c, ct) =>
    {
        c.HttpContext.Response.Headers.RetryAfter = "60";
        await c.HttpContext.Response.WriteAsJsonAsync(new { code = "RATE_LIMIT", message = "Aguarde um minuto antes de tentar novamente.", requestId = c.HttpContext.TraceIdentifier }, ct);
    };
});
var app = builder.Build();
if (bootstrap || verify)
{
    try
    {
        var init = app.Services.GetRequiredService<Inicializacao>();
        if (bootstrap)
            await init.Bootstrap();
        else
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(await init.Health()));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex is ApiException ? ex.Message : "Não foi possível preparar a API. Confira a conexão e o esquema do banco.");
        Environment.ExitCode = 1;
    }

    return;
}

if ((builder.Configuration["Security:IdempotencySecret"] ?? "").Length < 32)
    throw new InvalidOperationException("Execute Scripts/Configurar-Local.ps1 ou configure Security:IdempotencySecret com uma chave aleatória de pelo menos 32 caracteres.");
// Somente proxies locais conhecidos sao aceitos por padrao; nao confiar em headers externos arbitrarios.
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        ctx.Response.Headers.CacheControl = "no-store";
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    try
    {
        await next();
    }
    catch (OperationCanceledException)when (ctx.RequestAborted.IsCancellationRequested)
    {
        ctx.Response.StatusCode = 499;
    }
    catch (Exception ex)when (!ctx.Response.HasStarted)
    {
        var(status, code, message) = ex switch
        {
            ApiException e => (e.Status, e.Code, e.Message),
            MySqlException { Number: 1062 } => (409, "DUPLICATE", "Já existe um registro com estes dados. Atualize a tela e confira."),
            MySqlException { Number: 1205 or 1213 } => (409, "CONCURRENT_CHANGE", "Houve uma alteração simultânea. Repita com a mesma chave da requisição."),
            MySqlException { Number: 1451 or 1452 } => (409, "RELATION", "Um registro relacionado mudou ou não existe. Atualize a tela."),
            MySqlException { Number: 3819 or 1406 or 1048 or 1644 } => (422, "DATABASE_RULE", "Os dados não atendem às regras do banco. Confira os campos e o estado do registro."),
            MySqlException => (503, "DATABASE_UNAVAILABLE", "Não foi possível acessar o banco. Verifique a configuração da API."),
            _ => (500, "INTERNAL_ERROR", "Não foi possível concluir a operação.")};
        if (status >= 500)
            app.Logger.LogError(ex, "Falha {RequestId} na API", ctx.TraceIdentifier);
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { code, message, requestId = ctx.TraceIdentifier });
    }
});
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.Use(async (ctx, next) =>
{
    bool admin = ctx.Request.Path.StartsWithSegments("/api/v1/admin") || ctx.Request.Path.StartsWithSegments("/api/v1/auth/admin");
    bool write = ctx.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
    if (admin && write)
    {
        var origin = ctx.Request.Headers.Origin.ToString();
        var own = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        Regras.Exigir(origin.Length > 0 && (origin == own || origins.Contains(origin, StringComparer.Ordinal)), "Origem administrativa não autorizada.", 403);
        if (ctx.User.Identity?.IsAuthenticated == true && !ctx.Request.Path.Equals("/api/v1/auth/admin/login"))
        {
            var sent = ctx.Request.Headers["X-CSRF-Token"].ToString();
            var expected = ctx.Items["csrf-hash"] as byte[] ?? [];
            Regras.Exigir(sent.Length is >= 32 and <= 128 && CryptographicOperations.FixedTimeEquals(Autenticacao.HashToken(sent), expected), "Proteção CSRF inválida. Reabra a sessão.", 403);
        }
    }

    await next();
});
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/v1/health", async (Inicializacao init, CancellationToken ct) => Results.Ok(await init.Health(ct)));
app.MapGet("/openapi/v1.json", () => Results.File(Path.Combine(app.Environment.ContentRootPath, "Docs", "CONTRATO_API.openapi.json"), "application/json"));
// Opcional: servir a Web existente na mesma origem HTTPS. Nao altera nenhum arquivo dela.
var web = builder.Configuration["Web:Diretorio"];
if (!string.IsNullOrWhiteSpace(web))
{
    var path = Path.GetFullPath(web, app.Environment.ContentRootPath);
    if (!Directory.Exists(path))
        throw new InvalidOperationException("Web:Diretorio não existe.");
    var files = new PhysicalFileProvider(path);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
}

app.Run();
