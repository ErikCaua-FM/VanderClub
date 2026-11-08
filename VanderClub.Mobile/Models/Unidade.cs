namespace VanderClub.Models;

public sealed class Unidade : Entity
{
    public string FusoHorario { get; set; } = "America/Sao_Paulo";
    public string Nome { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Endereco { get; set; } = "";
    public override string ToString() => Nome;
}
