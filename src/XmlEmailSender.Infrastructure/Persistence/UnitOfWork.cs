using System.Data;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Infrastructure.Persistence;

/// <summary>
/// Implementación Dapper-friendly de IUnitOfWork.
/// Abre conexión + transacción bajo demanda; los repos las consumen vía las
/// propiedades Connection / Transaction. Una instancia por scope (request).
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _factory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IDbConnectionFactory factory) => _factory = factory;

    public IDbConnection Connection
        => _connection ??= _factory.CreateOpenConnection();

    public IDbTransaction? Transaction => _transaction;

    public Task BeginAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        if (_transaction != null) return Task.CompletedTask;
        _connection ??= _factory.CreateOpenConnection();
        _transaction = _connection.BeginTransaction();
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        if (_transaction == null)
            throw new InvalidOperationException("No hay transacción activa.");
        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        EnsureNotDisposed();
        if (_transaction == null) return Task.CompletedTask;
        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _transaction?.Dispose();
        _connection?.Dispose();
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));
    }
}
