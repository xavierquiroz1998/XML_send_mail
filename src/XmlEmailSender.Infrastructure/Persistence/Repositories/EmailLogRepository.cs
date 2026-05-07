using Dapper;
using XmlEmailSender.Domain.Emails;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Infrastructure.Persistence.Repositories;

internal sealed class EmailLogRepository : IEmailLogRepository
{
    private readonly IUnitOfWork _uow;

    public EmailLogRepository(IUnitOfWork uow) => _uow = uow;

    public Task AddAsync(EmailLog log, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO EmailLogs (Id, ElectronicDocumentId, RecipientEmail, Subject, Status, ErrorMessage, SentAt, CreatedAt, UpdatedAt)
VALUES (@Id, @ElectronicDocumentId, @RecipientEmail, @Subject, @Status, @ErrorMessage, @SentAt, @CreatedAt, @UpdatedAt);";

        return _uow.Connection.ExecuteAsync(sql, new
        {
            log.Id,
            log.ElectronicDocumentId,
            log.RecipientEmail,
            log.Subject,
            Status = (int)log.Status,
            log.ErrorMessage,
            log.SentAt,
            log.CreatedAt,
            log.UpdatedAt
        }, transaction: _uow.Transaction);
    }

    public Task UpdateAsync(EmailLog log, CancellationToken ct = default)
    {
        const string sql = @"
UPDATE EmailLogs SET
    Status = @Status,
    ErrorMessage = @ErrorMessage,
    SentAt = @SentAt,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id;";

        return _uow.Connection.ExecuteAsync(sql, new
        {
            log.Id,
            Status = (int)log.Status,
            log.ErrorMessage,
            log.SentAt,
            log.UpdatedAt
        }, transaction: _uow.Transaction);
    }

    public async Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM EmailLogs WHERE Id = @Id";
        var row = await _uow.Connection.QueryFirstOrDefaultAsync<Row>(
            sql, new { Id = id }, transaction: _uow.Transaction);
        return row == null ? null : Map(row);
    }

    public async Task<IReadOnlyList<EmailLog>> ListByDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM EmailLogs WHERE ElectronicDocumentId = @Id ORDER BY CreatedAt DESC";
        var rows = await _uow.Connection.QueryAsync<Row>(
            sql, new { Id = documentId }, transaction: _uow.Transaction);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<EmailLog>> ListAsync(int skip, int take, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM EmailLogs ORDER BY CreatedAt DESC LIMIT @Take OFFSET @Skip";
        var rows = await _uow.Connection.QueryAsync<Row>(
            sql, new { Skip = skip, Take = take }, transaction: _uow.Transaction);
        return rows.Select(Map).ToList();
    }

    private static EmailLog Map(Row r) => EmailLog.Hydrate(
        r.Id, r.ElectronicDocumentId, r.RecipientEmail, r.Subject,
        (EmailStatus)r.Status, r.ErrorMessage, r.SentAt, r.CreatedAt, r.UpdatedAt);

    private sealed class Row
    {
        public Guid Id { get; set; }
        public Guid ElectronicDocumentId { get; set; }
        public string RecipientEmail { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public int Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
