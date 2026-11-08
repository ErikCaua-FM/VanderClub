using VanderClub.Models;
namespace VanderClub.Helpers;

public sealed class AppState
{
    private const string TokenKey = "vanderclub.api.session.v2";
    public IClock Clock { get; } = new SystemClock();
    public ApiService Club { get; }
    public SQLiteDatabaseHelper Db { get; }
    public DateTime Hoje => Horario.Local(Clock.UtcNow).Date;
    public Contexto? Atual { get; private set; }
    public AppState()
    {
        Db = new SQLiteDatabaseHelper(Path.Combine(FileSystem.AppDataDirectory, "vanderclub_catalog_v2.db3"));
        Club = new ApiService(Db);
    }
    public async Task<bool> IniciarAsync()
    {
        await Club.InicializarAsync();
        // Remove os bancos fictícios da versão anterior, sem importar registros de laboratório.
        foreach (var name in new[] { "vanderclub_demo.db3", "vanderclub_cache.db3" })
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                try { File.Delete(Path.Combine(FileSystem.AppDataDirectory, name + suffix)); } catch (IOException) { }
        SecureStorage.Default.Remove("vc_session");
        string? token;
        try { token = await SecureStorage.Default.GetAsync(TokenKey); } catch { token = null; }
        if (string.IsNullOrEmpty(token) || !await Club.RestaurarSessaoAsync(token)) { await EncerrarLocalAsync(); return false; }
        await AtualizarAsync(); return true;
    }
    public async Task<bool> EntrarAsync(string email, string senha)
    {
        await EncerrarLocalAsync();
        var login = await Club.LoginAsync(email, senha);
        try { await AtualizarAsync(); } catch { await EncerrarLocalAsync(); throw; }
        try { await SecureStorage.Default.SetAsync(TokenKey, login.Token); return true; }
        catch { SecureStorage.Default.Remove(TokenKey); return false; }
    }
    public async Task AtualizarAsync() => Atual = await Club.ContextoAsync();
    public async Task EncerrarLocalAsync()
    {
        SecureStorage.Default.Remove(TokenKey); Club.ClearSession(); Atual = null; await Db.ClearAsync();
    }
    public async Task SairAsync()
    {
        try { await Club.LogoutAsync(); }
        catch (ApiException) { /* O dispositivo sempre encerra a sessão, mesmo sem rede. */ }
        finally { await EncerrarLocalAsync(); }
    }
}
