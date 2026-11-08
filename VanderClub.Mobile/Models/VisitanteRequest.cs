namespace VanderClub.Models;
public sealed record VisitanteRequest(string Nome, string TipoDocumento, string Documento, [property: System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))] DateTime Nascimento, string Telefone, string Dia, string Placa, string Observacao);
