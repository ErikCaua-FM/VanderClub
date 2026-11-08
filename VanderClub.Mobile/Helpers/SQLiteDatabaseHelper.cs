using System.Text.Json;
using SQLite;
using VanderClub.Models;
namespace VanderClub.Helpers;

// Cache descartável de catálogos, áreas e eventos. Não armazena senhas, tokens,
// perfis pessoais, votos, respostas de atendimento ou dados financeiros.
public sealed class SQLiteDatabaseHelper
{
    private readonly SQLiteAsyncConnection _db;
    private readonly Task _ready;
    public SQLiteDatabaseHelper(string path)
    {
        _db = new SQLiteAsyncConnection(path);
        _ready = _db.CreateTableAsync<CacheEntry>();
    }
    public async Task<T?> ReadAsync<T>(string key)
    {
        await _ready;
        var entry = await _db.FindAsync<CacheEntry>(key);
        if (entry is null) return default;
        if (entry.ExpiresUtcTicks <= DateTime.UtcNow.Ticks) { await _db.DeleteAsync(entry); return default; }
        try { return JsonSerializer.Deserialize<T>(entry.Json, ApiService.JsonOptions); }
        catch (JsonException) { await _db.DeleteAsync(entry); return default; }
    }
    public async Task SaveAsync<T>(string key, T value)
    {
        await _ready;
        await _db.InsertOrReplaceAsync(new CacheEntry { Key = key,
            Json = JsonSerializer.Serialize(value, ApiService.JsonOptions),
            ExpiresUtcTicks = DateTime.UtcNow.AddSeconds(30).Ticks });
    }
    public async Task ClearAsync() { await _ready; await _db.DeleteAllAsync<CacheEntry>(); }
}
