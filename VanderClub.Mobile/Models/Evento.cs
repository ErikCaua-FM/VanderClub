namespace VanderClub.Models;
public sealed class Evento : Entity
{
    public int UnidadeId { get; set; }
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Local { get; set; } = "";
    public DateTime InicioUtc { get; set; }
    public DateTime FimUtc { get; set; }
    public string Status { get; set; } = "Agendado";
    public string Publico { get; set; } = "Associados";
    public int Capacidade { get; set; }
    public string Imagem { get; set; } = "festa.png";
    public string MotivoCancelamento { get; set; } = "";
}
