namespace VanderClub.Models;

public sealed class Reserva : Entity
{
    public int UsuarioId { get; set; }
    public int AreaId { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FimUtc { get; set; }
    public string Status { get; set; } = "Confirmada";
    public DateTime CriadoUtc { get; set; }
    public string ChaveCliente { get; set; } = "";
    public string Protocolo => $"RES-{Id:D6}";
}
