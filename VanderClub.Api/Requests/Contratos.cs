using System.ComponentModel.DataAnnotations;

namespace VanderClub.Api.Requests;
public sealed class LoginRequest
{
    [Required(AllowEmptyStrings = true)]
    public string Email { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Senha { get; set; } = "";
}

public sealed class Cadastro
{
    [Required(AllowEmptyStrings = true)]
    public string Nome { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Email { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Senha { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cpf { get; set; } = "";
    public DateOnly Nascimento { get; set; } = default;

    [Required(AllowEmptyStrings = true)]
    public string Telefone { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Endereco { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cidade { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Uf { get; set; } = "";
    public int PlanoId { get; set; } = 0;
    public int UnidadeId { get; set; } = 0;
    public int PlanoVersaoId { get; set; } = 0;
    public int CondicaoPagamentoId { get; set; } = 0;

    [Required(AllowEmptyStrings = true)]
    public string Numero { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Bairro { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Complemento { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cep { get; set; } = "";
    public bool AceitouTermos { get; set; } = false;
}

public sealed class BeneficiarioRequest
{
    public int Versao { get; set; } = 0;

    [Required(AllowEmptyStrings = true)]
    public string Nome { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cpf { get; set; } = "";
    public DateOnly Nascimento { get; set; } = default;

    [Required(AllowEmptyStrings = true)]
    public string Telefone { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Tipo { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Parentesco { get; set; } = "";
}

public sealed class DadosPessoais
{
    public int Versao { get; set; } = 0;

    [Required(AllowEmptyStrings = true)]
    public string Nome { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Email { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cpf { get; set; } = "";
    public DateOnly Nascimento { get; set; } = default;

    [Required(AllowEmptyStrings = true)]
    public string Telefone { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Endereco { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Numero { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Complemento { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Bairro { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cidade { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Uf { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cep { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Sexo { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string EstadoCivil { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Interesses { get; set; } = "";
}

public sealed class ChamadoRequest
{
    [Required(AllowEmptyStrings = true)]
    public string Tipo { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Motivo { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Descricao { get; set; } = "";
}

public sealed class ReservaRequest
{
    public int AreaId { get; set; } = 0;
    public DateTimeOffset InicioUtc { get; set; } = default;
    public DateTimeOffset FimUtc { get; set; } = default;
}

public sealed class MensagemRequest
{
    [Required(AllowEmptyStrings = true)]
    public string Texto { get; set; } = "";
}

public sealed class VotoRequest
{
    public int OpcaoId { get; set; } = 0;
}

public sealed class VersaoRequest
{
    public int Versao { get; set; } = 0;
}

public sealed class VisitanteRequest
{
    [Required(AllowEmptyStrings = true)]
    public string Nome { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string TipoDocumento { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Documento { get; set; } = "";
    public DateOnly Nascimento { get; set; } = default;

    [Required(AllowEmptyStrings = true)]
    public string Telefone { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Dia { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Placa { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Observacao { get; set; } = "";
}

public sealed class AdminLogin
{
    [Required(AllowEmptyStrings = true)]
    public string Email { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Password { get; set; } = "";
}

public sealed class AdminUnitInput
{
    [Required(AllowEmptyStrings = true)]
    public string Name { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Acronym { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Description { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Street { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Number { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Complement { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string District { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string City { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Uf { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cep { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Phone { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Email { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Timezone { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Image { get; set; } = "";
    public bool Active { get; set; } = false;
    public int Version { get; set; } = 0;
}

public sealed class AdminAdministratorInput
{
    [Required(AllowEmptyStrings = true)]
    public string Name { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cpf { get; set; } = "";
    public DateOnly Birth { get; set; } = default;

    [Required(AllowEmptyStrings = true)]
    public string Phone { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Email { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Street { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Number { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Complement { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string District { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string City { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Uf { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Cep { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Gender { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string MaritalStatus { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Login { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Password { get; set; } = "";
    public string? UnitId { get; set; } = null;
    public bool Active { get; set; } = false;
    public int Version { get; set; } = 0;
}

public sealed class AdminEventInput
{
    [Required(AllowEmptyStrings = true)]
    public string Title { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string UnitId { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Description { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Status { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Audience { get; set; } = "";
    public int Capacity { get; set; } = 0;

    [Required(AllowEmptyStrings = true)]
    public string Location { get; set; } = "";
    public string? AreaId { get; set; } = null;

    [Required(AllowEmptyStrings = true)]
    public string Image { get; set; } = "";
    public DateTimeOffset Start { get; set; } = default;
    public DateTimeOffset End { get; set; } = default;
    public DateTimeOffset RegStart { get; set; } = default;
    public DateTimeOffset RegEnd { get; set; } = default;
    public int Version { get; set; } = 0;
}

public sealed class AdminPollInput
{
    [Required(AllowEmptyStrings = true)]
    public string UnitId { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Title { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Description { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Rules { get; set; } = "";
    public DateTimeOffset Start { get; set; } = default;
    public DateTimeOffset End { get; set; } = default;
    public int Quorum { get; set; } = 0;
    public bool RequirePaid { get; set; } = false;
    public bool Closed { get; set; } = false;

    [Required(AllowEmptyStrings = true)]
    public List<string> Options { get; set; } = [];
}

public sealed class AdminPlanInput
{
    [Required(AllowEmptyStrings = true)]
    public string Name { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Code { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Description { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Scope { get; set; } = "";
    public int Quota { get; set; } = 0;
    public int DependentLimit { get; set; } = 0;
    public int GuestLimit { get; set; } = 0;
    public int DurationMonths { get; set; } = 0;
    public int GraceDays { get; set; } = 0;
    public int FutureReservationsLimit { get; set; } = 0;

    [Required(AllowEmptyStrings = true)]
    public string Benefits { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Terms { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public List<PlanConditionInput> Conditions { get; set; } = [];
    public int Version { get; set; } = 0;
}

public sealed class PlanConditionInput
{
    [Required(AllowEmptyStrings = true)]
    public string Name { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Method { get; set; } = "";
    public int Installments { get; set; } = 0;
    public long TotalCents { get; set; } = 0;
}

public sealed class VersionInput
{
    public int Version { get; set; } = 0;
}

public sealed class ReasonInput
{
    public int Version { get; set; } = 0;

    [Required(AllowEmptyStrings = true)]
    public string Reason { get; set; } = "";
}

public sealed class ReviewInput
{
    public int Version { get; set; } = 0;

    [Required(AllowEmptyStrings = true)]
    public string Decision { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Reason { get; set; } = "";
}

public sealed class TextInput
{
    public int Version { get; set; } = 0;

    [Required(AllowEmptyStrings = true)]
    public string Text { get; set; } = "";
}

public sealed class PinInput
{
    public int Version { get; set; } = 0;
    public bool Pinned { get; set; } = false;
}

public sealed class PaymentInput
{
    public int Version { get; set; } = 0;
    public DateTimeOffset ReceivedAt { get; set; } = default;

    [Required(AllowEmptyStrings = true)]
    public string Method { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Reference { get; set; } = "";

    [Required(AllowEmptyStrings = true)]
    public string Note { get; set; } = "";
}
