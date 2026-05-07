using Dapper;
using Microsoft.Extensions.Logging;

namespace XmlEmailSender.Infrastructure.Persistence.Schema;

/// <summary>
/// Mini-runner de migraciones para Dapper.
/// - Lee scripts SQL embedidos como recursos (Persistence/Schema/Scripts).
/// - Filtra por sufijo según provider (.sqlite.sql / .postgres.sql).
/// - Aplica los pendientes en orden alfabético y los registra en __schema_migrations.
/// Postgres y SQLite ejecutan el script completo en una sola llamada (no requieren
/// batch splitting tipo "GO" de SQL Server).
/// </summary>
public sealed class SchemaMigrationRunner
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<SchemaMigrationRunner> _logger;

    public SchemaMigrationRunner(IDbConnectionFactory factory, ILogger<SchemaMigrationRunner> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var providerSuffix = _factory.Provider switch
        {
            DatabaseProvider.Sqlite => ".sqlite.sql",
            DatabaseProvider.Postgres => ".postgres.sql",
            _ => throw new NotSupportedException()
        };

        using var conn = await _factory.CreateOpenConnectionAsync(ct);

        await EnsureMigrationsTableAsync(conn);

        var applied = (await conn.QueryAsync<string>(
            "SELECT script_name FROM __schema_migrations")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var assembly = typeof(SchemaMigrationRunner).Assembly;
        var prefix = $"{assembly.GetName().Name}.Persistence.Schema.Scripts.";
        var scripts = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal))
            .Where(n => n.EndsWith(providerSuffix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        if (scripts.Count == 0)
        {
            _logger.LogWarning("No se encontraron scripts de schema para el provider {Provider}.", _factory.Provider);
            return;
        }

        foreach (var resource in scripts)
        {
            var scriptName = resource.Substring(prefix.Length);
            if (applied.Contains(scriptName))
            {
                _logger.LogDebug("Migración ya aplicada: {Script}", scriptName);
                continue;
            }

            _logger.LogInformation("Aplicando migración: {Script}", scriptName);
            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"No se pudo abrir el recurso {resource}.");
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(ct);

            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(sql, transaction: tx);
                await conn.ExecuteAsync(
                    "INSERT INTO __schema_migrations (script_name, applied_at) VALUES (@name, @at)",
                    new { name = scriptName, at = DateTime.UtcNow },
                    transaction: tx);
                tx.Commit();
                _logger.LogInformation("Migración aplicada: {Script}", scriptName);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }

    private async Task EnsureMigrationsTableAsync(System.Data.IDbConnection conn)
    {
        var sql = _factory.Provider switch
        {
            DatabaseProvider.Sqlite =>
                @"CREATE TABLE IF NOT EXISTS __schema_migrations (
                    script_name TEXT PRIMARY KEY,
                    applied_at  TEXT NOT NULL
                  );",
            DatabaseProvider.Postgres =>
                @"CREATE TABLE IF NOT EXISTS __schema_migrations (
                    script_name VARCHAR(255) PRIMARY KEY,
                    applied_at  TIMESTAMPTZ NOT NULL
                  );",
            _ => throw new NotSupportedException()
        };
        await conn.ExecuteAsync(sql);
    }
}
