namespace VanderClub.Models;
public sealed record BeneficiarioRequest(int Versao, string Nome, string Cpf, [property: System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))] DateTime Nascimento, string Telefone, string Tipo, string Parentesco);
