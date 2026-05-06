using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Domain.Repositories;

public interface IElectronicDocumentRepository
{
    Task AddAsync(ElectronicDocument document, CancellationToken ct = default);
    Task<ElectronicDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ElectronicDocument?> GetByAccessKeyAsync(string accessKey, CancellationToken ct = default);
    Task<bool> ExistsByAccessKeyAsync(string accessKey, CancellationToken ct = default);
    Task<IReadOnlyList<ElectronicDocument>> ListAsync(int skip, int take, CancellationToken ct = default);
}
