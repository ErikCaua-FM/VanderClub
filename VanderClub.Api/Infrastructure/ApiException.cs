namespace VanderClub.Api.Infrastructure;
public sealed class ApiException(int status, string message, string code = "VALIDATION") : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;

    public static ApiException Missing() => new(404, "Registro não encontrado neste acesso.", "NOT_FOUND");
    public static ApiException Conflict(string message = "Este registro mudou. Atualize antes de continuar.") => new(409, message, "CONFLICT");
}
