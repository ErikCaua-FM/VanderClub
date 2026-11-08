namespace VanderClub.Models;

public sealed class Mensalidade : Entity
{
    public int UsuarioId { get; set; }
    public int Numero { get; set; }
    public int TotalParcelas { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))]
    public DateTime Vencimento { get; set; }
    public long ValorCentavos { get; set; }
    public DateTime? PagoUtc { get; set; }
    public bool PagamentoSimulado { get; set; }
    public string Parcela => $"{Numero:D2}/{TotalParcelas:D2}";
}
