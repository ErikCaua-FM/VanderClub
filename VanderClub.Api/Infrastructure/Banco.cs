using System.Data;
using Dapper;
using MySqlConnector;
using VanderClub.Api.Models;

namespace VanderClub.Api.Infrastructure;
public sealed class Banco(IConfiguration config)
{
    public async Task<Trabalho> Abrir(CancellationToken ct = default)
    {
        var value = config.GetConnectionString("VanderClub");
        if (string.IsNullOrWhiteSpace(value))
            throw new ApiException(503, "Configure a conexão VanderClub na API.", "DATABASE_CONFIG");
        var b = new MySqlConnectionStringBuilder(value)
        {
            DateTimeKind = MySqlDateTimeKind.Utc,
            AllowUserVariables = false
        };
        var c = new MySqlConnection(b.ConnectionString);
        try
        {
            await c.OpenAsync(ct);
            await c.ExecuteAsync("SET SESSION time_zone='+00:00'");
            return new(c, ct);
        }
        catch
        {
            await c.DisposeAsync();
            throw;
        }
    }
}

public sealed class Trabalho(MySqlConnection connection, CancellationToken cancellation = default) : IAsyncDisposable
{
    public MySqlConnection Connection { get; } = connection;
    public MySqlTransaction? Transaction { get; private set; }
    public CancellationToken Cancellation { get; } = cancellation;

    public async Task Begin(IsolationLevel level = IsolationLevel.ReadCommitted) => Transaction = await Connection.BeginTransactionAsync(level, Cancellation);
    public async Task Commit()
    {
        if (Transaction is not null)
        {
            await Transaction.CommitAsync(Cancellation);
            await Transaction.DisposeAsync();
            Transaction = null;
        }
    }

    public async Task Rollback()
    {
        if (Transaction is not null)
        {
            await Transaction.RollbackAsync(Cancellation);
            await Transaction.DisposeAsync();
            Transaction = null;
        }
    }

    CommandDefinition Cmd(string sql, object? p = null) => new(sql, p, Transaction, commandTimeout: 20, cancellationToken: Cancellation);
    public Task<int> Exec(string sql, object? p = null) => Connection.ExecuteAsync(Cmd(sql, p));
    public Task<T?> Scalar<T>(string sql, object? p = null) => Connection.ExecuteScalarAsync<T>(Cmd(sql, p));
    public async Task<List<Registro>> Rows(string sql, object? p = null) => (await Connection.QueryAsync(Cmd(sql, p))).Select(v => new Registro((IDictionary<string, object?>)v)).ToList();
    public async Task<Registro?> One(string sql, object? p = null) => (await Rows(sql, p)).SingleOrDefault();
    public async Task<Registro> Required(string sql, object? p = null) => await One(sql, p) ?? throw ApiException.Missing();
    // Nomes de tabelas/colunas sao constantes do codigo; valores sempre parametros.
    public async Task<int> Insert(string table, object values)
    {
        var names = values.GetType().GetProperties().Select(p => p.Name).ToArray();
        return await Scalar<int>($"INSERT INTO `{table}` ({string.Join(',', names.Select(n => $"`{n}`"))}) VALUES ({string.Join(',', names.Select(n => "@" + n))}); SELECT LAST_INSERT_ID();", values);
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction is not null)
            await Transaction.DisposeAsync();
        await Connection.DisposeAsync();
    }
}

public sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => DateOnly.FromDateTime(Convert.ToDateTime(value));
    public override void SetValue(IDbDataParameter p, DateOnly value)
    {
        p.Value = value.ToDateTime(TimeOnly.MinValue);
        p.DbType = DbType.Date;
    }
}
