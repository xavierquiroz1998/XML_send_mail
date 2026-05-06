using Dapper;
using XmlEmailSender.Domain.Emails;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Infrastructure.Persistence.Repositories;

internal sealed class SmtpConfigurationRepository : ISmtpConfigurationRepository
{
    private readonly IUnitOfWork _uow;

    public SmtpConfigurationRepository(IUnitOfWork uow) => _uow = uow;

    public async Task<SmtpConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM SmtpConfigurations WHERE Id = @Id";
        var row = await _uow.Connection.QueryFirstOrDefaultAsync<Row>(
            sql, new { Id = id }, transaction: _uow.Transaction);
        return row == null ? null : Map(row);
    }

    public async Task<SmtpConfiguration?> GetActiveAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM SmtpConfigurations WHERE IsActive = 1";
        var row = await _uow.Connection.QueryFirstOrDefaultAsync<Row>(
            sql, transaction: _uow.Transaction);
        return row == null ? null : Map(row);
    }

    public async Task<IReadOnlyList<SmtpConfiguration>> ListAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM SmtpConfigurations ORDER BY Name";
        var rows = await _uow.Connection.QueryAsync<Row>(sql, transaction: _uow.Transaction);
        return rows.Select(Map).ToList();
    }

    public Task AddAsync(SmtpConfiguration config, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO SmtpConfigurations
    (Id, Name, Host, Port, UseSsl, Username, EncryptedPassword, FromEmail, FromName, IsActive, CreatedAt, UpdatedAt)
VALUES
    (@Id, @Name, @Host, @Port, @UseSsl, @Username, @EncryptedPassword, @FromEmail, @FromName, @IsActive, @CreatedAt, @UpdatedAt);";

        return _uow.Connection.ExecuteAsync(sql, new
        {
            config.Id, config.Name, config.Host, config.Port,
            UseSsl = config.UseSsl ? 1 : 0,
            config.Username, config.EncryptedPassword, config.FromEmail, config.FromName,
            IsActive = config.IsActive ? 1 : 0,
            config.CreatedAt, config.UpdatedAt
        }, transaction: _uow.Transaction);
    }

    public Task UpdateAsync(SmtpConfiguration config, CancellationToken ct = default)
    {
        const string sql = @"
UPDATE SmtpConfigurations SET
    Name = @Name,
    Host = @Host,
    Port = @Port,
    UseSsl = @UseSsl,
    Username = @Username,
    EncryptedPassword = @EncryptedPassword,
    FromEmail = @FromEmail,
    FromName = @FromName,
    IsActive = @IsActive,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id;";

        return _uow.Connection.ExecuteAsync(sql, new
        {
            config.Id, config.Name, config.Host, config.Port,
            UseSsl = config.UseSsl ? 1 : 0,
            config.Username, config.EncryptedPassword, config.FromEmail, config.FromName,
            IsActive = config.IsActive ? 1 : 0,
            config.UpdatedAt
        }, transaction: _uow.Transaction);
    }

    public Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM SmtpConfigurations WHERE Id = @Id";
        return _uow.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _uow.Transaction);
    }

    private static SmtpConfiguration Map(Row r) => SmtpConfiguration.Hydrate(
        r.Id, r.Name, r.Host, r.Port, r.UseSsl != 0, r.Username, r.EncryptedPassword,
        r.FromEmail, r.FromName, r.IsActive != 0, r.CreatedAt, r.UpdatedAt);

    private sealed class Row
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public int UseSsl { get; set; }
        public string Username { get; set; } = null!;
        public string EncryptedPassword { get; set; } = null!;
        public string FromEmail { get; set; } = null!;
        public string FromName { get; set; } = null!;
        public int IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
