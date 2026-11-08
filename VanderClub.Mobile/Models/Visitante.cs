namespace VanderClub.Models;

public sealed class Visitante : Entity
{
    public int UsuarioId { get; set; }
    public int UnidadeId { get; set; }
    public string Nome { get; set; } = "";
    public string TipoDocumento { get; set; } = "CPF";
    public string Documento { get; set; } = "";
    [System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))]
    public DateTime Nascimento { get; set; }
    public string Telefone { get; set; } = "";
    public string Dia { get; set; } = "";
    public string Placa { get; set; } = "";
    public string Observacao { get; set; } = "";
    public bool Cancelado { get; set; }
}
