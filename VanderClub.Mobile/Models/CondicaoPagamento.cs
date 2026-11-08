using VanderClub.Helpers;
namespace VanderClub.Models;
public sealed class CondicaoPagamento
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Meio { get; set; } = "";
    public int Parcelas { get; set; }
    public long ValorTotalCentavos { get; set; }
    public override string ToString() => Parcelas <= 1
        ? $"{Nome}: {Horario.Dinheiro(ValorTotalCentavos)} • {Meio}"
        : $"{Nome}: {Parcelas} parcelas • total {Horario.Dinheiro(ValorTotalCentavos)} • {Meio}";
}
