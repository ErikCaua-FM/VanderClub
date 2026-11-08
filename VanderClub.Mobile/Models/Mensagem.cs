namespace VanderClub.Models;

public sealed class Mensagem : Entity
{
    public int UnidadeId { get; set; }
    public int? AutorUsuarioId { get; set; }
    public string ClasseAutor { get; set; } = "";
    public string AutorNome { get; set; } = "";
    public bool Administrador { get; set; }
    public string Texto { get; set; } = "";
    public DateTime CriadoUtc { get; set; }
    public string ChaveCliente { get; set; } = "";
}
