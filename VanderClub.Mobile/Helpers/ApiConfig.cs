namespace VanderClub.Helpers;

public static class ApiConfig
{
    // Defina a URL da API na próxima etapa. Ex.: https://api.seudominio.com/api/v1/
    // Nenhuma credencial, senha ou conexão MySQL pertence ao aplicativo.
    public const string BaseUrl = "https://localhost:7147/api/v1/";
    public static TimeSpan Timeout => TimeSpan.FromSeconds(25);
    public static Uri Endpoint(string relativePath)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri))
            throw new RegraException("O serviço do VanderClub ainda não foi configurado. Tente novamente após a disponibilização da API.");
        if (uri.Scheme != "https")
            throw new RegraException("Configure um endereço HTTPS válido para o serviço do VanderClub.");
        return new Uri(new Uri(uri.AbsoluteUri.TrimEnd('/') + "/"), relativePath);
    }
}
