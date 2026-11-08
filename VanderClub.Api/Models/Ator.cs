using System.Security.Claims;
using VanderClub.Api.Infrastructure;

namespace VanderClub.Api.Models;
public sealed record Ator(int Id, int PessoaId, string Nome, string Perfil, int UnidadeId, int SessaoId)
{
    public bool Geral => Perfil == "ADM_GERAL";
    public bool Admin => Perfil is "ADM_GERAL" or "ADM_LOCAL";

    public void ExigirGeral() => Regras.Exigir(Geral, "Ação exclusiva do Presidente.", 403);
    public void ExigirUnidade(int id) => Regras.Exigir(Geral || (Perfil == "ADM_LOCAL" && UnidadeId == id), "Acesso não autorizado a esta unidade.", 403);
    public static Ator From(HttpContext c)
    {
        var u = c.User;
        Regras.Exigir(u.Identity?.IsAuthenticated == true, "Entre novamente na sua conta.", 401);
        int N(string k) => int.TryParse(u.FindFirstValue(k), out var n) ? n : 0;
        return new(N(ClaimTypes.NameIdentifier), N("pessoa"), u.FindFirstValue(ClaimTypes.Name) ?? "", u.FindFirstValue(ClaimTypes.Role) ?? "", N("unidade"), N("sessao"));
    }
}
