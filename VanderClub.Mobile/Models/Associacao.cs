namespace VanderClub.Models;

public sealed class Associacao : Entity
{
    public string Beneficios { get; set; } = "";
    public string Regras { get; set; } = "";
    public int VersaoPlano { get; set; }
    public string Status { get; set; } = "";
    public int UsuarioId { get; set; }
    public int PlanoId { get; set; }
    public int UnidadeOrigemId { get; set; }
    public string PlanoNome { get; set; } = "";
    public bool TodaRede { get; set; }
    public int LimiteBeneficiarios { get; set; }
    public string Condicao { get; set; } = "Parcelado";
    public long ValorTotalCentavos { get; set; }
    public int Parcelas { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))]
    public DateTime InicioLocal { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))]
    public DateTime FimLocal { get; set; }
}
