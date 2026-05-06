using System.Data;

namespace XmlEmailSender.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    DatabaseProvider Provider { get; }
    IDbConnection CreateOpenConnection();
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
