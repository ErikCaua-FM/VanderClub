namespace VanderClub.Models;

public sealed class Votacao : Entity
{
    public int UnidadeId { get; set; }
    public string Motivo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public DateTime InicioUtc { get; set; }
    public DateTime FimUtc { get; set; }
    public bool Encerrada { get; set; }
}
