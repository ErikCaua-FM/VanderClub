namespace VanderClub.Models;

public sealed class Plano : Entity
{
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public bool TodaRede { get; set; }
    public int LimiteBeneficiarios { get; set; }
    public int VersaoId { get; set; }
    public int DuracaoMeses { get; set; }
    public string Beneficios { get; set; } = "";
    public string Regras { get; set; } = "";
    public List<CondicaoPagamento> Condicoes { get; set; } = [];
}
