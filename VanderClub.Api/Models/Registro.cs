using System.Globalization;

namespace VanderClub.Api.Models;
// Linha interna do banco. Nunca serializar diretamente para os clientes.
public sealed class Registro(IDictionary<string, object?> values)
{
    public object? Get(string key) => values.TryGetValue(key, out var v) && v is not DBNull ? v : null;
    public string S(string key) => Convert.ToString(Get(key), CultureInfo.InvariantCulture) ?? "";
    public int I(string key) => Get(key)is null ? 0 : Convert.ToInt32(Get(key), CultureInfo.InvariantCulture);
    public long L(string key) => Get(key)is null ? 0L : Convert.ToInt64(Get(key), CultureInfo.InvariantCulture);
    public bool B(string key) => Get(key)is not null && Convert.ToBoolean(Get(key), CultureInfo.InvariantCulture);
    public DateTime D(string key) => DateTime.SpecifyKind(Convert.ToDateTime(Get(key), CultureInfo.InvariantCulture), DateTimeKind.Utc);
    public DateTime? DN(string key) => Get(key)is null ? null : D(key);
    public DateOnly Day(string key) => DateOnly.FromDateTime(D(key));
    public string Date(string key) => Get(key)is null ? "" : Day(key).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string Iso(string key) => Get(key)is null ? "" : D(key).ToString("O", CultureInfo.InvariantCulture);
    public string Ids(string key = "id") => Get(key)is null ? "" : I(key).ToString(CultureInfo.InvariantCulture);
    public byte[] Bytes(string key) => (byte[])(Get(key) ?? Array.Empty<byte>());
    public int Id => I("id");
}
