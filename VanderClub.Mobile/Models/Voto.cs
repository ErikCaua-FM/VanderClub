namespace VanderClub.Models;

public sealed class Voto : Entity
{
    public int VotacaoId { get; set; }
    public int UsuarioId { get; set; }
    public int OpcaoId { get; set; }
    public DateTime CriadoUtc { get; set; }
}
