using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using VanderClub.Models;
namespace VanderClub.Helpers;

/// <summary>Único acesso aos dados remotos. Contrato completo em CONTRATO_API.md.</summary>
public sealed class ApiService(SQLiteDatabaseHelper cache, HttpClient? httpClient = null, Func<string, Uri>? endpoint = null)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { TypeInfoResolver = ApiJsonContext.Default };
    private readonly HttpClient _http = httpClient ?? new HttpClient { Timeout = ApiConfig.Timeout };
    private readonly Dictionary<string, string> _pendingKeys = new();
    private string? _token;
    private int _userId, _unitId;
    private string UnitPath => $"mobile/units/{_unitId}";
    public void ClearSession() { _token = null; _userId = 0; _unitId = 0; _pendingKeys.Clear(); }
    public Task LimparCacheAsync() => cache.ClearAsync();
    public Task InicializarAsync() => cache.ClearAsync();

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, string? key = null)
    {
        using var request = new HttpRequestMessage(method, (endpoint ?? ApiConfig.Endpoint)(path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (_token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        string json = body is null ? "" : JsonSerializer.Serialize(body, JsonOptions);
        if (body is not null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        string fingerprint = method + ":" + path + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        bool mutation = method != HttpMethod.Get && !path.StartsWith("auth/");
        if (mutation)
        {
            if (!_pendingKeys.TryGetValue(fingerprint, out var retained))
                _pendingKeys[fingerprint] = retained = key ?? Guid.NewGuid().ToString();
            request.Headers.Add("Idempotency-Key", retained);
        }
        try
        {
            using var response = await _http.SendAsync(request);
            string text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                string message = (int)response.StatusCode switch {
                    401 => "Sua sessão expirou ou os dados de acesso são inválidos.",
                    403 => "Você não tem permissão para realizar esta ação.",
                    409 => "Este registro foi alterado ou o horário não está disponível. Atualize e confira os dados.",
                    429 => "Muitas solicitações. Aguarde um momento e tente novamente.",
                    _ => "O serviço não conseguiu concluir a solicitação. Tente novamente." };
                string? requestId = null;
                try {
                    using var error = JsonDocument.Parse(text);
                    if (error.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        message = msg.GetString() ?? message;
                    if (error.RootElement.TryGetProperty("requestId", out var id)) requestId = id.GetString();
                } catch (JsonException) { }
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) { ClearSession(); await cache.ClearAsync(); }
                if (response.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.UnprocessableEntity)
                    _pendingKeys.Remove(fingerprint);
                throw new ApiException((int)response.StatusCode, message, requestId);
            }
            T value = string.IsNullOrWhiteSpace(text) ? default! :
                JsonSerializer.Deserialize<T>(text, JsonOptions)!;
            if (mutation) { _pendingKeys.Remove(fingerprint); await cache.ClearAsync(); }
            return value;
        }
        catch (HttpRequestException) { throw new ApiException(0, "Não foi possível acessar o serviço. Confira sua conexão e tente novamente."); }
        catch (TaskCanceledException) { throw new ApiException(0, "O serviço demorou para responder. Confira o registro antes de tentar novamente."); }
        catch (JsonException) { throw new ApiException(0, "O serviço retornou um formato incompatível. Contate a administração."); }
    }
    private Task<T> Get<T>(string path) => SendAsync<T>(HttpMethod.Get, path);
    private async Task Write(HttpMethod method, string path, object? data = null, string? key = null)
        => await SendAsync<JsonElement>(method, path, data, key);
    private async Task<List<T>> Catalog<T>(string path)
    {
        var key = $"{_userId}:{_unitId}:{path}";
        var cached = await cache.ReadAsync<List<T>>(key);
        if (cached is not null) return cached;
        var value = await Get<List<T>>(path);
        if (value is null) throw new ApiException(0, "O catálogo não foi recebido. Tente novamente.");
        await cache.SaveAsync(key, value); return value;
    }
    public async Task<LoginResult> LoginAsync(string email, string senha)
    {
        email = Validacao.Email(email);
        if (string.IsNullOrWhiteSpace(senha)) throw new RegraException("Informe sua senha.");
        ClearSession(); await cache.ClearAsync();
        var result = await SendAsync<LoginResult>(HttpMethod.Post, "auth/mobile/login", new LoginRequest(email, senha));
        if (result is null || string.IsNullOrWhiteSpace(result.Token)) throw new ApiException(0, "O serviço não iniciou uma sessão válida.");
        _token = result.Token; _userId = result.Perfil.Id; return result;
    }
    public async Task<bool> RestaurarSessaoAsync(string token)
    {
        _token = token;
        try { var context = await ContextoAsync(); _userId = context.Perfil.Id; return true; }
        catch { ClearSession(); return false; }
    }
    public async Task LogoutAsync()
    {
        try { if (_token is not null) await Write(HttpMethod.Post, "auth/mobile/logout"); }
        finally { ClearSession(); await cache.ClearAsync(); }
    }
    public Task CadastrarAsync(Cadastro c) => Write(HttpMethod.Post, "mobile/registrations", c);
    public Task<List<Plano>> PlanosAsync() => Catalog<Plano>("public/plans");
    public Task<List<Unidade>> UnidadesAsync() => Catalog<Unidade>(_token is null ? "public/units" : "mobile/units");
    public async Task<Contexto> ContextoAsync()
    {
        var c = await Get<Contexto>("mobile/context" + (_unitId == 0 ? "" : $"?unitId={_unitId}"));
        if (c is null) throw new ApiException(0, "Não foi possível carregar sua associação.");
        _unitId = c.Unidade.Id; _userId = c.Perfil.Id; Horario.UsarFuso(c.Unidade.FusoHorario); return c;
    }
    public async Task MudarUnidadeAsync(int unidadeId)
    {
        var c = await Get<Contexto>($"mobile/context?unitId={unidadeId}");
        _unitId = c.Unidade.Id; Horario.UsarFuso(c.Unidade.FusoHorario); await cache.ClearAsync();
    }
    public Task<List<Beneficiario>> BeneficiariosAsync() => Get<List<Beneficiario>>("mobile/beneficiaries");
    public Task SalvarBeneficiarioAsync(Beneficiario b) => Write(b.Id == 0 ? HttpMethod.Post : HttpMethod.Put, "mobile/beneficiaries" + (b.Id == 0 ? "" : $"/{b.Id}"), new BeneficiarioRequest(b.Versao, b.Nome, b.Cpf, b.Nascimento, b.Telefone, b.Tipo, b.Parentesco));
    public Task ExcluirBeneficiarioAsync(int id, int versao) => Write(HttpMethod.Delete, $"mobile/beneficiaries/{id}", new VersaoRequest(versao));
    public Task<List<Visitante>> VisitantesAsync(DateTime dia) => Get<List<Visitante>>($"{UnitPath}/visitors?date={dia:yyyy-MM-dd}");
    public Task SalvarVisitanteAsync(Visitante v) => Write(HttpMethod.Post, $"{UnitPath}/visitors", new VisitanteRequest(v.Nome, v.TipoDocumento, v.Documento, v.Nascimento, v.Telefone, v.Dia, v.Placa, v.Observacao));
    public Task CancelarVisitanteAsync(int id) => Write(HttpMethod.Post, $"{UnitPath}/visitors/{id}/cancel");
    public Task<List<Chamado>> ChamadosAsync(string tipo) => Get<List<Chamado>>($"{UnitPath}/tickets?type={Uri.EscapeDataString(tipo)}");
    public Task<Chamado> ChamadoAsync(int id) => Get<Chamado>($"{UnitPath}/tickets/{id}");
    public async Task<int> CriarChamadoAsync(string tipo, string motivo, string descricao)
        => (await SendAsync<Chamado>(HttpMethod.Post, $"{UnitPath}/tickets", new ChamadoRequest(tipo, motivo, descricao))).Id;
    public Task<List<Area>> AreasAsync() => Task.FromResult(CatalogoAreas.Listar(_unitId));
    public Task<Area> AreaAsync(int id) => Task.FromResult(CatalogoAreas.Obter(_unitId, id));
    public Task<List<Reserva>> AgendaAsync(int areaId, DateTime inicioLocal, DateTime fimLocal)
        => Get<List<Reserva>>($"{UnitPath}/areas/{areaId}/availability?start={Uri.EscapeDataString(Horario.Utc(inicioLocal).ToString("O"))}&end={Uri.EscapeDataString(Horario.Utc(fimLocal).ToString("O"))}");
    public Task<List<ItemReserva>> MinhasReservasAsync() => Get<List<ItemReserva>>($"{UnitPath}/reservations");
    public async Task<Reserva> ReservarAsync(int areaId, DateTime inicioLocal, DateTime fimLocal, string chave)
    {
        var area = await AreaAsync(areaId);
        if (inicioLocal >= fimLocal) throw new RegraException("A saída deve ser posterior à entrada.");
        if (Horario.Utc(inicioLocal) <= DateTime.UtcNow) throw new RegraException("Escolha um horário futuro.");
        if (area.MaxMinutos > 0 && (fimLocal - inicioLocal).TotalMinutes > area.MaxMinutos)
            throw new RegraException($"Este espaço permite reservas de até {area.MaxMinutos / 60} horas.");
        return await SendAsync<Reserva>(HttpMethod.Post, $"{UnitPath}/reservations",
            new ReservaRequest(areaId, Horario.Utc(inicioLocal), Horario.Utc(fimLocal)), chave);
    }
    public Task CancelarReservaAsync(int id) => Write(HttpMethod.Post, $"{UnitPath}/reservations/{id}/cancel");
    public Task<List<Mensagem>> MensagensAsync(int antesId = 0) => Get<List<Mensagem>>($"{UnitPath}/messages?beforeId={antesId}&limit=50");
    public Task EnviarMensagemAsync(string texto, string chave) => Write(HttpMethod.Post, $"{UnitPath}/messages", new MensagemRequest(Validacao.Obrigatorio(texto, "a mensagem", 2000)), chave);
    public Task<List<Votacao>> VotacoesAsync() => Get<List<Votacao>>($"{UnitPath}/polls");
    public Task<DetalheVotacao> VotacaoAsync(int id) => Get<DetalheVotacao>($"{UnitPath}/polls/{id}");
    public Task VotarAsync(int votacaoId, int opcaoId) => Write(HttpMethod.Post, $"{UnitPath}/polls/{votacaoId}/vote", new VotoRequest(opcaoId));
    public Task SimularPagamentoAsync(int id, string chave) => Write(HttpMethod.Post, $"mobile/invoices/{id}/simulate-payment", key: chave);
    public Task<List<Mensalidade>> MensalidadesAsync() => Get<List<Mensalidade>>("mobile/invoices");
    public Task<List<Evento>> EventosAsync() => Catalog<Evento>($"{UnitPath}/events");
    public Task<Evento> EventoAsync(int id) => Get<Evento>($"{UnitPath}/events/{id}");
    public Task<MinhaConta> MinhaContaAsync() => Get<MinhaConta>("mobile/me");
    public Task AtualizarContaAsync(DadosPessoais dados) => Write(HttpMethod.Put, "mobile/me", dados);
}
