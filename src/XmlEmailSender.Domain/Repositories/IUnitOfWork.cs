using System.Data;

namespace XmlEmailSender.Domain.Repositories;

/// <summary>
/// Maneja la transacción atómica para una operación de negocio (Dapper).
/// Cada operación abre una conexión, ejecuta los repos, y luego CommitAsync/RollbackAsync.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }

    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
