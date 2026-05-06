using XmlEmailSender.Domain.Emails;

namespace XmlEmailSender.Domain.Repositories;

public interface ISmtpConfigurationRepository
{
    Task<SmtpConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SmtpConfiguration?> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SmtpConfiguration>> ListAsync(CancellationToken ct = default);
    Task AddAsync(SmtpConfiguration config, CancellationToken ct = default);
    Task UpdateAsync(SmtpConfiguration config, CancellationToken ct = default);
    Task RemoveAsync(Guid id, CancellationToken ct = default);
}
