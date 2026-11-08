namespace VanderClub.Models;

public sealed class Chamado : Entity
{
    public int UsuarioId { get; set; }
    public int UnidadeId { get; set; }
    public string Tipo { get; set; } = "Solicitação";
    public string Motivo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string? Status { get; set; }
    public string RespostaAdmin { get; set; } = "";
    public DateTime CriadoUtc { get; set; }
    public DateTime? RespondidoUtc { get; set; }
    public string Protocolo => $"{(Tipo == "Solicitação" ? "SOL" : "REC")}-{CriadoUtc:yyyy}-{Id:D5}";
}
