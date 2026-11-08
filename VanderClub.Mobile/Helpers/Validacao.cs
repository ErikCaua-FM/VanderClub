using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using VanderClub.Models;

namespace VanderClub.Helpers;

public interface IClock { DateTime UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
public static class Horario
{
    private static TimeZoneInfo Zona = FindZone();
    public static void UsarFuso(string id)
    {
        try { Zona = TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { throw new RegraException("Fuso horário da unidade não reconhecido."); }
    }
    private static TimeZoneInfo FindZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
    public static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);
    public static DateTime Utc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zona);
    public static string DataHora(DateTime utc) => Local(utc).ToString("dd/MM/yyyy HH:mm");
    public static string Dinheiro(long centavos) => (centavos / 100m).ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    public static string Status(Mensalidade m, DateTime hoje) => m.PagoUtc.HasValue ? "Pago" : m.Vencimento.Date < hoje.Date ? "Atrasado" : "Pendente";
}
public static class Validacao
{
    public static string Obrigatorio(string? valor, string campo, int max = 200)
    {
        var s = valor?.Trim() ?? "";
        if (s.Length == 0) throw new RegraException($"Informe {campo}.");
        if (s.Length > max) throw new RegraException($"{campo} deve ter até {max} caracteres.");
        return s;
    }
    public static string Digitos(string? s) => string.Concat((s ?? "").Where(char.IsDigit));
    public static string Email(string s)
    {
        s = Obrigatorio(s, "o e-mail", 254).ToLowerInvariant();
        if (!MailAddress.TryCreate(s, out var parsed) || parsed.Address != s || !s.Contains('.'))
            throw new RegraException("Informe um e-mail válido.");
        return s;
    }
    public static string Cpf(string s)
    {
        s = Digitos(s);
        if (s.Length != 11 || s.Distinct().Count() == 1) throw new RegraException("Informe um CPF válido (11 dígitos).");
        for (int count = 9; count <= 10; count++)
        {
            int soma = 0;
            for (int i = 0; i < count; i++) soma += (s[i] - '0') * (count + 1 - i);
            int resto = soma % 11, d = resto < 2 ? 0 : 11 - resto;
            if (d != s[count] - '0') throw new RegraException("Os dígitos verificadores do CPF não conferem.");
        }
        return s;
    }
    public static string Telefone(string s)
    {
        var n = Digitos(s);
        if (n.Length < 10 || n.Length > 13) throw new RegraException("Informe o telefone com DDD (10 a 13 dígitos).");
        return n;
    }
    public static DateTime Nascimento(DateTime data, DateTime hoje, bool maior = false)
    {
        data = data.Date;
        if (data > hoje.Date || data < hoje.AddYears(-120).Date) throw new RegraException("Confira a data de nascimento.");
        if (maior && data > hoje.AddYears(-18).Date) throw new RegraException("O titular deve ter pelo menos 18 anos.");
        return data;
    }
}
