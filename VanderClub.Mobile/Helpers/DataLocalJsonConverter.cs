using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace VanderClub.Helpers;

// Nascimento, vencimento e vigência são datas civis, sem conversão de fuso horário.
public sealed class DataLocalJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && DateTime.TryParseExact(reader.GetString(), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date;
        throw new JsonException("A data deve usar o formato yyyy-MM-dd.");
    }
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
