namespace VanderClub.Models;
public sealed class DadosPessoais
{
    public int Versao { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cpf { get; set; } = "";
    [System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))]
    public DateTime Nascimento { get; set; }
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Complemento { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Cep { get; set; } = "";
    public string Sexo { get; set; } = "";
    public string EstadoCivil { get; set; } = "";
    public string Interesses { get; set; } = "";
}
