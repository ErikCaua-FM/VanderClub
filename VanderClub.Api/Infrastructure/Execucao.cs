using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using VanderClub.Api.Models;

namespace VanderClub.Api.Infrastructure;
public sealed class Execucao(Banco banco, IConfiguration config)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public byte[] Hash(string data)
    {
        var secret = config["Security:IdempotencySecret"] ?? "";
        if (secret.Length < 32)
            throw new ApiException(503, "Configure Security:IdempotencySecret na API.", "SECURITY_CONFIG");
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(data));
    }

    public async Task<IActionResult> Read(HttpContext ctx, Func<Trabalho, Ator, Task<object>> action)
    {
        await using var w = await banco.Abrir(ctx.RequestAborted);
        await w.Begin();
        var result = await action(w, Ator.From(ctx));
        await w.Commit();
        return new OkObjectResult(result);
    }

    public async Task<IActionResult> Public(HttpContext ctx, Func<Trabalho, Task<object>> action)
    {
        await using var w = await banco.Abrir(ctx.RequestAborted);
        return new OkObjectResult(await action(w));
    }

    public async Task<IActionResult> Write(HttpContext ctx, object? body, Func<Trabalho, Ator, Task<object?>> action, int success = 204, string? registrationEmail = null)
    {
        var a = registrationEmail is null ? Ator.From(ctx) : new Ator(0, 0, "", "ASSOCIADO", 0, 0);
        var key = ctx.Request.Headers["Idempotency-Key"].ToString();
        Regras.Exigir(key.Length is >= 8 and <= 100 && key.All(c => c >= 33 && c <= 126), "Envie uma Idempotency-Key de 8 a 100 caracteres.", 400);
        var scope = registrationEmail is null ? "usuario:" + a.Id : "adesao:" + Convert.ToHexString(Hash(registrationEmail));
        var route = ctx.Request.Path.ToString();
        var method = ctx.Request.Method;
        var payload = Hash(JsonSerializer.Serialize(body, Json));
        await using var w = await banco.Abrir(ctx.RequestAborted);
        await w.Begin();
        int id;
        try
        {
            id = await w.Insert("requisicoes_idempotentes", new { usuario_id = a.Id == 0 ? (int? )null : a.Id, escopo_autenticacao = scope, rota = route, metodo = method, chave = key, payload_hash = payload, status = "PROCESSANDO", expira_em = DateTime.UtcNow.AddDays(7) });
        }
        catch (MySqlException ex)when (ex.Number == 1062)
        {
            var existing = await w.Required("SELECT * FROM requisicoes_idempotentes WHERE escopo_autenticacao=@scope AND rota=@route AND metodo=@method AND chave=@key FOR UPDATE", new { scope, route, method, key });
            if (!CryptographicOperations.FixedTimeEquals(existing.Bytes("payload_hash"), payload))
                throw ApiException.Conflict("Esta chave já foi usada com outros dados.");
            if (existing.S("status") != "CONCLUIDA")
                throw ApiException.Conflict("Requisição em processamento. Tente novamente com a mesma chave.");
            await w.Commit();
            return Stored(existing.I("http_status"), existing.S("resposta"));
        }

        var value = await action(w, a);
        var serialized = JsonSerializer.Serialize(value, Json);
        await w.Exec("UPDATE requisicoes_idempotentes SET status='CONCLUIDA',http_status=@success,resposta=@serialized WHERE id=@id", new { success, serialized, id });
        await w.Commit();
        return Stored(success, serialized);
    }

    static IActionResult Stored(int code, string value) => code == 204 ? new NoContentResult() : new ContentResult
    {
        StatusCode = code,
        ContentType = "application/json; charset=utf-8",
        Content = value
    };
    public static Task<int> Audit(Trabalho w, Ator a, string action, string entity, int id, int? unit = null, string? note = null) => w.Insert("auditoria", new { ator_usuario_id = a.Id > 0 ? (int? )a.Id : null, clube_id = unit, origem = a.Id > 0 ? "USUARIO" : "SISTEMA", acao = action, entidade = entity, referencia = id.ToString(), correlacao_id = Guid.NewGuid().ToString(), observacao = note });
}
