namespace VanderClub.Models;

public sealed record Perfil(int Id, string Nome, string Email, string TipoSocio)
{
    public bool Titular => TipoSocio == "Titular";
    public string Iniciais => string.Concat(Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(n => n[0])).ToUpperInvariant();
}
