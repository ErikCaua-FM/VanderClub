namespace VanderClub.Models;

public sealed class Area : Entity
{
    public int UnidadeId { get; set; }
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Regras { get; set; } = "";
    public string Imagem { get; set; } = "";
    public int Capacidade { get; set; }
    public int MaxMinutos { get; set; } // 0 = sem limite de duração para salões.
    public int AbreMinuto { get; set; }
    public int FechaMinuto { get; set; }
    public string LimiteTexto => MaxMinutos == 0 ? "Duração livre" : $"Até {MaxMinutos / 60} horas";
}
