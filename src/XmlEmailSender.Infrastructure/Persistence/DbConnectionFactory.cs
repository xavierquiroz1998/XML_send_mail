using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using XmlEmailSender.Infrastructure.Persistence.TypeHandlers;

namespace XmlEmailSender.Infrastructure.Persistence;

internal sealed class DbConnectionFactory : IDbConnectionFactory
{
    private static int _handlersRegistered;
    private readonly string _connectionString;

    public DatabaseProvider Provider { get; }

    public DbConnectionFactory(DatabaseProvider provider, string connectionString)
    {
        Provider = provider;
        _connectionString = connectionString;
        EnsureDapperHandlers(provider);
    }

    /// <summary>
    /// Registra los Dapper type handlers requeridos por SQLite (Guid/DateTime como TEXT).
    /// Idempotente: usa Interlocked para que múltiples factories no compitan.
    /// </summary>
    private static void EnsureDapperHandlers(DatabaseProvider provider)
    {
        if (provider != DatabaseProvider.Sqlite) return;
        if (Interlocked.CompareExchange(ref _handlersRegistered, 1, 0) != 0) return;

        DefaultTypeMap.MatchNamesWithUnderscores = false;
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        SqlMapper.AddTypeHandler(new NullableGuidTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler());
    }

    public IDbConnection CreateOpenConnection()
    {
        IDbConnection conn = Provider switch
        {
            DatabaseProvider.Sqlite => new SqliteConnection(_connectionString),
            DatabaseProvider.SqlServer => new SqlConnection(_connectionString),
            _ => throw new NotSupportedException($"Provider no soportado: {Provider}")
        };
        conn.Open();
        return conn;
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        switch (Provider)
        {
            case DatabaseProvider.Sqlite:
                var sqlite = new SqliteConnection(_connectionString);
                await sqlite.OpenAsync(ct);
                return sqlite;
            case DatabaseProvider.SqlServer:
                var sql = new SqlConnection(_connectionString);
                await sql.OpenAsync(ct);
                return sql;
            default:
                throw new NotSupportedException($"Provider no soportado: {Provider}");
        }
    }
}
