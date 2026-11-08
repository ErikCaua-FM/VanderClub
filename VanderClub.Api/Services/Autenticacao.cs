using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using VanderClub.Api.Infrastructure;
using VanderClub.Api.Models;

namespace VanderClub.Api.Services;
public sealed class Autenticacao(Banco banco, IConfiguration config)
{
    public const string Cookie = "vanderclub_admin";
    readonly PasswordHasher<string> hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210000 }));
    static readonly Lazy<string> Dummy = new(() => new PasswordHasher<string>(Options.Create(new PasswordHasherOptions { IterationCount = 210000 })).HashPassword("", Convert.ToHexString(RandomNumberGenerator.GetBytes(32))));
    public string HashPassword(string password) => hasher.HashPassword("", Regras.Senha(password));
    public static byte[] HashToken(string s) => SHA256.HashData(Encoding.UTF8.GetBytes(s));
    public static string Csrf(string token) => WebEncoders.Base64UrlEncode(HMACSHA256.HashData(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes("vanderclub-csrf-v1")));
    public CookieOptions CookieOptions(DateTime? expires = null) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = config["Security:CookieSameSite"] == "None" ? SameSiteMode.None : SameSiteMode.Lax,
        Path = "/api/v1/",
        Expires = expires,
        IsEssential = true
    };
    public const string UserSql = """
        SELECT u.*,p.nome,p.cpf,p.email_contato,p.nascimento,p.telefone,p.logradouro,p.numero,p.complemento,p.bairro,p.cep,p.sexo,p.estado_civil,
               ci.nome AS cidade,e.uf,ac.clube_id AS admin_unidade_id
        FROM usuarios u JOIN pessoas p ON p.id=u.pessoa_id
        LEFT JOIN cidades ci ON ci.id=p.cidade_id LEFT JOIN estados e ON e.id=ci.estado_id
        LEFT JOIN administradores_clubes ac ON ac.usuario_id=u.id AND ac.ativo=1
        """;
    public static object AdminActor(Registro u, bool personal = true) => new
    {
        id = u.Ids(),
        name = u.S("nome"),
        login = u.S("email_login"),
        email = u.S("email_contato"),
        unitId = u.I("admin_unidade_id") == 0 ? "" : u.Ids("admin_unidade_id"),
        role = u.S("perfil"),
        active = u.S("status") == "ATIVO",
        cpf = personal ? u.S("cpf") : "",
        birth = personal ? u.Date("nascimento") : "",
        phone = u.S("telefone"),
        street = personal ? u.S("logradouro") : "",
        number = personal ? u.S("numero") : "",
        complement = personal ? u.S("complemento") : "",
        district = personal ? u.S("bairro") : "",
        city = personal ? u.S("cidade") : "",
        uf = personal ? u.S("uf") : "",
        cep = personal ? u.S("cep") : "",
        gender = personal ? u.S("sexo") : "",
        maritalStatus = personal ? u.S("estado_civil") : "",
        version = u.I("versao")
    };
    public async Task<(string token, Registro user)> Login(string email, string password, bool admin, CancellationToken ct)
    {
        email = Regras.Email(email);
        Regras.Exigir(!string.IsNullOrEmpty(password) && password.Length <= 128, "E-mail ou senha incorretos.", 401);
        await using var w = await banco.Abrir(ct);
        await w.Begin();
        var u = await w.One("SELECT * FROM usuarios WHERE email_normalizado=@email FOR UPDATE", new { email });
        var locked = u?.DN("bloqueado_ate")is DateTime until && until > DateTime.UtcNow;
        PasswordVerificationResult verified;
        try
        {
            verified = hasher.VerifyHashedPassword("", u?.S("senha_hash") ?? Dummy.Value, password);
        }
        catch (FormatException)
        {
            verified = PasswordVerificationResult.Failed;
        }

        var roleValid = u is not null && (admin ? u.S("perfil")is "ADM_GERAL" or "ADM_LOCAL" : u.S("perfil") == "ASSOCIADO");
        if (u is null || verified == PasswordVerificationResult.Failed || locked || !roleValid || u.S("status") != "ATIVO")
        {
            if (u is not null && !locked)
            {
                var count = u.DN("bloqueado_ate")is DateTime d && d <= DateTime.UtcNow ? 1 : u.I("falhas_login") + 1;
                await w.Exec("UPDATE usuarios SET falhas_login=@count,bloqueado_ate=@blocked WHERE id=@id", new { count, blocked = count >= 5 ? DateTime.UtcNow.AddMinutes(10) : (DateTime? )null, id = u.Id });
            }

            await w.Commit();
            throw new ApiException(401, "E-mail ou senha incorretos. Se houve várias tentativas, aguarde 10 minutos.", "INVALID_LOGIN");
        }

        var user = await w.Required(UserSql + " WHERE u.id=@id", new { id = u.Id });
        if (admin && user.S("perfil") == "ADM_LOCAL")
            Regras.Exigir(user.I("admin_unidade_id") > 0, "Seu vínculo administrativo está inativo.", 403);
        if (!admin)
        {
            var member = await w.One("SELECT m.id FROM membros_contrato m JOIN contratos c ON c.id=m.contrato_id WHERE m.pessoa_id=@pid AND m.status='ATIVO' AND c.status IN ('VIGENTE','SUSPENSO','ENCERRADO','PENDENTE') ORDER BY c.id DESC LIMIT 1", new { pid = user.I("pessoa_id") });
            Regras.Exigir(member is not null, "Seu cadastro ainda não está aprovado para acesso.", 403);
        }

        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        await w.Insert("sessoes", new { usuario_id = u.Id, token_hash = HashToken(token), csrf_token_hash = admin ? HashToken(Csrf(token)) : null, plataforma = admin ? "WEB" : "MOBILE", expira_em = DateTime.UtcNow.AddHours(Math.Clamp(config.GetValue("Security:SessionHours", 8), 1, 24)) });
        var hash = verified == PasswordVerificationResult.SuccessRehashNeeded ? HashPassword(password) : u.S("senha_hash");
        await w.Exec("UPDATE usuarios SET ultimo_login_em=UTC_TIMESTAMP(6),falhas_login=0,bloqueado_ate=NULL,senha_hash=@hash WHERE id=@id", new { hash, id = u.Id });
        user = await w.Required(UserSql + " WHERE u.id=@id", new { id = u.Id });
        await w.Commit();
        return (token, user);
    }

    public async Task<Registro?> Session(string token, bool admin, CancellationToken ct)
    {
        if (token.Length is < 32 or > 256)
            return null;
        await using var w = await banco.Abrir(ct);
        return await w.One("""
            SELECT s.id AS sessao_id,s.csrf_token_hash,u.id,u.pessoa_id,u.perfil,p.nome,ac.clube_id AS admin_unidade_id
            FROM sessoes s JOIN usuarios u ON u.id=s.usuario_id JOIN pessoas p ON p.id=u.pessoa_id
            LEFT JOIN administradores_clubes ac ON ac.usuario_id=u.id AND ac.ativo=1
            WHERE s.token_hash=@hash AND s.plataforma=@platform AND s.revogada_em IS NULL
              AND s.expira_em>UTC_TIMESTAMP(6) AND u.status='ATIVO' AND p.inativado_em IS NULL
              AND ((@admin=1 AND (u.perfil='ADM_GERAL' OR (u.perfil='ADM_LOCAL' AND ac.clube_id IS NOT NULL)))
                OR (@admin=0 AND u.perfil='ASSOCIADO'))
            """, new { hash = HashToken(token), platform = admin ? "WEB" : "MOBILE", admin });
    }

    public async Task Logout(Ator a, CancellationToken ct)
    {
        await using var w = await banco.Abrir(ct);
        await w.Exec("UPDATE sessoes SET revogada_em=UTC_TIMESTAMP(6) WHERE id=@id AND revogada_em IS NULL", new { id = a.SessaoId });
    }
}
