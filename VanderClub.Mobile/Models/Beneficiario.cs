namespace VanderClub.Models;

public sealed class Beneficiario : Entity
{
    public int Versao { get; set; }
    public int TitularId { get; set; }
    public int? UsuarioVinculadoId { get; set; }
    public string Nome { get; set; } = "";
    public string Cpf { get; set; } = "";
    [System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))]
    public DateTime Nascimento { get; set; }
    public string Telefone { get; set; } = "";
    public string Tipo { get; set; } = "Convidado";
    public string Parentesco { get; set; } = "";
    public string Status { get; set; } = "Pendente";
    public string ObservacaoAdmin { get; set; } = "";
    public DateTime CriadoUtc { get; set; }
}
