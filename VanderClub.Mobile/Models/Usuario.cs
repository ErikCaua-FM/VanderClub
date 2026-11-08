namespace VanderClub.Models;

public sealed class Usuario : Entity
{
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cpf { get; set; } = "";
    [System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))]
    public DateTime Nascimento { get; set; }
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "";
    public string TipoSocio { get; set; } = "Titular";
    public int? ResponsavelId { get; set; }
    public DateTime CriadoUtc { get; set; }
}
