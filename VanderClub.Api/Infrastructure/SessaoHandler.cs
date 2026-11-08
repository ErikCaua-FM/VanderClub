using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using VanderClub.Api.Services;

namespace VanderClub.Api.Infrastructure;
public sealed class SessaoHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, Autenticacao auth) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var admin = Request.Path.StartsWithSegments("/api/v1/admin") || Request.Path.StartsWithSegments("/api/v1/auth/admin");
        var token = admin ? Request.Cookies[Autenticacao.Cookie] : Request.Headers.Authorization.ToString();
        if (!admin)
            token = token?.StartsWith("Bearer ", StringComparison.Ordinal) == true ? token[7..] : null;
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();
        var s = await auth.Session(token, admin, Context.RequestAborted);
        if (s is null)
            return AuthenticateResult.Fail("Sessão inválida.");
        Context.Items["session-token"] = token;
        Context.Items["csrf-hash"] = s.Bytes("csrf_token_hash");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, s.Ids()),
            new Claim(ClaimTypes.Name, s.S("nome")),
            new Claim(ClaimTypes.Role, s.S("perfil")),
            new Claim("pessoa", s.Ids("pessoa_id")),
            new Claim("unidade", s.Ids("admin_unidade_id")),
            new Claim("sessao", s.Ids("sessao_id"))
        };
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        await Response.WriteAsJsonAsync(new { code = "UNAUTHENTICATED", message = "Entre novamente na sua conta.", requestId = Context.TraceIdentifier });
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        await Response.WriteAsJsonAsync(new { code = "FORBIDDEN", message = "Você não tem permissão para esta operação.", requestId = Context.TraceIdentifier });
    }
}
