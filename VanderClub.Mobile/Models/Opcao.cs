namespace VanderClub.Models;

public sealed class Opcao : Entity
{
    public int VotacaoId { get; set; }
    public string Texto { get; set; } = "";
}
