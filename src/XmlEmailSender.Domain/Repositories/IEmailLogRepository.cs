using XmlEmailSender.Domain.Emails;

namespace XmlEmailSender.Domain.Repositories;

public interface IEmailLogRepository
{
    Task AddAsync(EmailLog log, CancellationToken ct = default);
    Task UpdateAsync(EmailLog log, CancellationToken ct = default);
    Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmailLog>> ListByDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<EmailLog>> ListAsync(int skip, int take, CancellationToken ct = default);
}
