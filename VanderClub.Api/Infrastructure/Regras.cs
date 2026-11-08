using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace VanderClub.Api.Infrastructure;
public static class Regras
{
    public const long MaxCentavos = 999999999999;
    public static void Exigir(bool condition, string message, int status = 422)
    {
        if (!condition)
            throw new ApiException(status, message);
    }

    public static string Texto(string? s, string campo, int max = 200, int min = 1)
    {
        s = (s ?? "").Trim();
        Exigir(s.Length >= min && s.Length <= max, $"Confira {campo}: de {min} a {max} caracteres.");
        return s;
    }

    public static string Opcional(string? s, int max = 500)
    {
        s = (s ?? "").Trim();
        Exigir(s.Length <= max, $"Texto maior que {max} caracteres.");
        return s;
    }

    public static string Digitos(string? s) => Regex.Replace(s ?? "", "[^0-9]", "");
    public static string Email(string? s)
    {
        var e = Texto(s, "o e-mail", 254).ToLowerInvariant();
        Exigir(MailAddress.TryCreate(e, out var a) && a.Address == e, "Informe um e-mail válido.");
        return e;
    }

    public static string Senha(string? s)
    {
        Exigir(s is not null && s.Length is >= 8 and <= 128, "A senha precisa ter de 8 a 128 caracteres.");
        return s!;
    }

    public static string Cpf(string? s)
    {
        var n = Digitos(s);
        Exigir(n.Length == 11 && n.Distinct().Count() > 1, "CPF inválido.");
        for (int len = 9; len <= 10; len++)
        {
            int sum = 0;
            for (int i = 0; i < len; i++)
                sum += (n[i] - '0') * (len + 1 - i);
            var d = (sum * 10) % 11;
            if (d == 10)
                d = 0;
            Exigir(d == n[len] - '0', "CPF inválido.");
        }

        return n;
    }

    public static string Phone(string? s, bool required = true)
    {
        var v = Digitos(s);
        Exigir(!required && v.Length == 0 || v.Length is >= 10 and <= 15, "Telefone inválido.");
        return v;
    }

    public static string Cep(string? s)
    {
        var v = Digitos(s);
        Exigir(v.Length == 8, "CEP deve conter 8 dígitos.");
        return v;
    }

    public static DateOnly Birth(DateOnly d, bool adult = false)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Exigir(d >= today.AddYears(-120) && d <= today, "Data de nascimento inválida.");
        if (adult)
            Exigir(d <= today.AddYears(-18), "O titular/administrador deve ser maior de idade.");
        return d;
    }

    public static int Id(string? s)
    {
        Exigir(int.TryParse(s, out var n) && n > 0, "Identificador inválido.", 400);
        return n;
    }

    public static void Version(Models.Registro r, int version)
    {
        if (version < 1 || r.I("versao") != version)
            throw ApiException.Conflict();
    }

    public static DateTime Utc(DateTimeOffset t)
    {
        Exigir(t.Year >= 2000 && t.Year <= 2100, "Data e horário inválidos.");
        return t.UtcDateTime;
    }

    public static DateOnly Day(string? s)
    {
        Exigir(DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d), "Data inválida; use yyyy-MM-dd.", 400);
        return d;
    }

    public static TimeZoneInfo Zone(string s)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(s);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ApiException(422, "Fuso horário IANA inválido.");
        }
    }

    public static DateOnly Today(string zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone(zone)));
    public static string Protocolo(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N");
    public static string MemberType(string type) => type switch
    {
        "TITULAR" => "Titular",
        "DEPENDENTE" => "Dependente",
        "CONVIDADO" => "Convidado",
        _ => type
    };
    public static string Approval(string state) => state switch
    {
        "ATIVO" => "Aprovado",
        "PENDENTE" => "Pendente",
        "REJEITADO" => "Recusado",
        "SUSPENSO" => "Suspenso",
        "REMOVIDO" => "Removido",
        _ => state
    };
    public static string ContractStatus(string s) => s switch
    {
        "VIGENTE" => "Ativo",
        "SUSPENSO" => "Suspenso",
        "CANCELADO" => "Cancelado",
        "ENCERRADO" => "Encerrado",
        _ => "Pendente"
    };
    public static string Classe(string s) => s switch
    {
        "ADM_GERAL" => "ADM Geral",
        "ADM_LOCAL" => "ADM Local",
        _ => "Associado"
    };
    public static string Image(string? value, string fallback = "social.svg")
    {
        var s = Opcional(value, 500);
        if (s.Length == 0)
            return fallback;
        Exigir(Regex.IsMatch(s, @"^[a-zA-Z0-9_\-]+\.(svg|png|jpg|jpeg)$"), "Use o nome de um recurso de imagem do aplicativo.");
        return s;
    }
}
