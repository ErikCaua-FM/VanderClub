namespace VanderClub.Helpers;
public sealed class ApiException(int statusCode, string mensagem, string? requestId = null) : RegraException(mensagem)
{
    public int StatusCode { get; } = statusCode;
    public string? RequestId { get; } = requestId;
}
