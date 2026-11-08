namespace VanderClub.Models;

public sealed record Cadastro(string Nome, string Email, string Senha, string Cpf, [property: System.Text.Json.Serialization.JsonConverter(typeof(VanderClub.Helpers.DataLocalJsonConverter))] DateTime Nascimento,
    string Telefone, string Endereco, string Cidade, string Uf, int PlanoId, int UnidadeId,
    int PlanoVersaoId, int CondicaoPagamentoId, string Numero, string Bairro, string Complemento, string Cep,
    bool AceitouTermos);
