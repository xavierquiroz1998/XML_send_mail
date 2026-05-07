using Dapper;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Domain.Emails;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Infrastructure.Persistence.Repositories;

internal sealed class ElectronicDocumentRepository : IElectronicDocumentRepository
{
    private readonly IUnitOfWork _uow;

    public ElectronicDocumentRepository(IUnitOfWork uow) => _uow = uow;

    public async Task AddAsync(ElectronicDocument document, CancellationToken ct = default)
    {
        const string insertDoc = @"
INSERT INTO ElectronicDocuments (
    Id, Type, AccessKey, DocumentNumber, IssueDate, Environment,
    Issuer_Ruc, Issuer_BusinessName, Issuer_CommercialName, Issuer_Address,
    Receiver_IdType, Receiver_Id, Receiver_Name, Receiver_Email, Receiver_Phone, Receiver_Address,
    Subtotal, Taxes, Total, OriginalXml, CreatedAt, UpdatedAt
) VALUES (
    @Id, @Type, @AccessKey, @DocumentNumber, @IssueDate, @Environment,
    @IssuerRuc, @IssuerBusinessName, @IssuerCommercialName, @IssuerAddress,
    @ReceiverIdType, @ReceiverId, @ReceiverName, @ReceiverEmail, @ReceiverPhone, @ReceiverAddress,
    @Subtotal, @Taxes, @Total, @OriginalXml, @CreatedAt, @UpdatedAt
);";

        await _uow.Connection.ExecuteAsync(insertDoc, new
        {
            document.Id,
            Type = (int)document.Type,
            AccessKey = document.AccessKey.Value,
            document.DocumentNumber,
            document.IssueDate,
            document.Environment,
            IssuerRuc = document.Issuer.Ruc,
            IssuerBusinessName = document.Issuer.BusinessName,
            IssuerCommercialName = document.Issuer.CommercialName,
            IssuerAddress = document.Issuer.Address,
            ReceiverIdType = document.Receiver.IdentificationType,
            ReceiverId = document.Receiver.Identification,
            ReceiverName = document.Receiver.Name,
            ReceiverEmail = document.Receiver.Email,
            ReceiverPhone = document.Receiver.Phone,
            ReceiverAddress = document.Receiver.Address,
            document.Subtotal,
            document.Taxes,
            document.Total,
            document.OriginalXml,
            document.CreatedAt,
            document.UpdatedAt
        }, transaction: _uow.Transaction);

        if (document.Lines.Count > 0)
        {
            const string insertLine = @"
INSERT INTO DocumentLines (Id, ElectronicDocumentId, Code, Description, Quantity, UnitPrice, Discount, Subtotal)
VALUES (@Id, @ElectronicDocumentId, @Code, @Description, @Quantity, @UnitPrice, @Discount, @Subtotal);";

            await _uow.Connection.ExecuteAsync(insertLine, document.Lines, transaction: _uow.Transaction);
        }

        if (document.TaxBreakdown.Count > 0)
        {
            const string insertBucket = @"
INSERT INTO DocumentTaxBuckets (Id, ElectronicDocumentId, CodigoPorcentaje, BaseImponible, Valor)
VALUES (@Id, @ElectronicDocumentId, @CodigoPorcentaje, @BaseImponible, @Valor);";

            var bucketRows = document.TaxBreakdown.Select(b => new
            {
                Id = Guid.NewGuid(),
                ElectronicDocumentId = document.Id,
                b.CodigoPorcentaje,
                b.BaseImponible,
                b.Valor
            });
            await _uow.Connection.ExecuteAsync(insertBucket, bucketRows, transaction: _uow.Transaction);
        }
    }

    public async Task<ElectronicDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM ElectronicDocuments WHERE Id = @Id";
        var row = await _uow.Connection.QueryFirstOrDefaultAsync<DocumentRow>(
            sql, new { Id = id }, transaction: _uow.Transaction);
        if (row == null) return null;

        var lines = await LoadLinesAsync(id);
        var emails = await LoadEmailLogsAsync(id);
        var buckets = await LoadTaxBucketsAsync(id);
        return Materialize(row, lines, emails, buckets);
    }

    public async Task<ElectronicDocument?> GetByAccessKeyAsync(string accessKey, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM ElectronicDocuments WHERE AccessKey = @AccessKey";
        var row = await _uow.Connection.QueryFirstOrDefaultAsync<DocumentRow>(
            sql, new { AccessKey = accessKey }, transaction: _uow.Transaction);
        if (row == null) return null;

        var lines = await LoadLinesAsync(row.Id);
        var emails = await LoadEmailLogsAsync(row.Id);
        var buckets = await LoadTaxBucketsAsync(row.Id);
        return Materialize(row, lines, emails, buckets);
    }

    public async Task<bool> ExistsByAccessKeyAsync(string accessKey, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM ElectronicDocuments WHERE AccessKey = @AccessKey";
        var count = await _uow.Connection.ExecuteScalarAsync<long>(
            sql, new { AccessKey = accessKey }, transaction: _uow.Transaction);
        return count > 0;
    }

    public async Task<IReadOnlyList<ElectronicDocument>> ListAsync(int skip, int take, CancellationToken ct = default)
    {
        // SQLite y Postgres comparten la sintaxis LIMIT/OFFSET.
        const string sql = "SELECT * FROM ElectronicDocuments ORDER BY IssueDate DESC LIMIT @Take OFFSET @Skip";
        var rows = await _uow.Connection.QueryAsync<DocumentRow>(
            sql, new { Skip = skip, Take = take }, transaction: _uow.Transaction);

        return rows.Select(r => Materialize(
            r,
            Array.Empty<DocumentLine>(),
            Array.Empty<EmailLog>(),
            Array.Empty<TaxBucket>())).ToList();
    }

    private async Task<IEnumerable<TaxBucket>> LoadTaxBucketsAsync(Guid documentId)
    {
        const string sql = "SELECT CodigoPorcentaje, BaseImponible, Valor FROM DocumentTaxBuckets WHERE ElectronicDocumentId = @Id";
        var rows = await _uow.Connection.QueryAsync<TaxBucketRow>(
            sql, new { Id = documentId }, transaction: _uow.Transaction);
        return rows.Select(r => new TaxBucket(r.CodigoPorcentaje, r.BaseImponible, r.Valor));
    }

    private async Task<IEnumerable<DocumentLine>> LoadLinesAsync(Guid documentId)
    {
        const string sql = "SELECT * FROM DocumentLines WHERE ElectronicDocumentId = @Id";
        var rows = await _uow.Connection.QueryAsync<LineRow>(
            sql, new { Id = documentId }, transaction: _uow.Transaction);
        return rows.Select(r =>
        {
            var line = new DocumentLine(r.Code ?? string.Empty, r.Description, r.Quantity, r.UnitPrice, r.Discount, r.Subtotal);
            // Reasignar Id y FK para reflejar el estado real de la base
            typeof(DocumentLine).GetProperty(nameof(DocumentLine.Id))!.SetValue(line, r.Id);
            line.GetType().GetMethod("AttachTo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(line, new object[] { r.ElectronicDocumentId });
            return line;
        });
    }

    private async Task<IEnumerable<EmailLog>> LoadEmailLogsAsync(Guid documentId)
    {
        const string sql = "SELECT * FROM EmailLogs WHERE ElectronicDocumentId = @Id ORDER BY CreatedAt DESC";
        var rows = await _uow.Connection.QueryAsync<EmailLogRow>(
            sql, new { Id = documentId }, transaction: _uow.Transaction);
        return rows.Select(r => EmailLog.Hydrate(
            r.Id, r.ElectronicDocumentId, r.RecipientEmail, r.Subject,
            (EmailStatus)r.Status, r.ErrorMessage, r.SentAt, r.CreatedAt, r.UpdatedAt));
    }

    private static ElectronicDocument Materialize(
        DocumentRow row,
        IEnumerable<DocumentLine> lines,
        IEnumerable<EmailLog> emails,
        IEnumerable<TaxBucket> taxBuckets)
    {
        var issuer = new Issuer(
            row.Issuer_Ruc,
            row.Issuer_BusinessName,
            row.Issuer_CommercialName ?? string.Empty,
            row.Issuer_Address ?? string.Empty);

        var receiver = new Receiver(
            row.Receiver_IdType,
            row.Receiver_Id,
            row.Receiver_Name,
            row.Receiver_Email,
            row.Receiver_Phone,
            row.Receiver_Address);

        return ElectronicDocument.Hydrate(
            row.Id,
            (DocumentType)row.Type,
            row.AccessKey,
            row.DocumentNumber,
            row.IssueDate,
            row.Environment,
            issuer,
            receiver,
            row.Subtotal,
            row.Taxes,
            row.Total,
            row.OriginalXml,
            row.CreatedAt,
            row.UpdatedAt,
            lines,
            emails,
            taxBuckets);
    }

    // Filas planas que Dapper materializa por convención de nombre de columna.
#pragma warning disable IDE1006 // Naming styles — los nombres deben coincidir con las columnas SQL.
    private sealed class DocumentRow
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public string AccessKey { get; set; } = null!;
        public string DocumentNumber { get; set; } = null!;
        public DateTime IssueDate { get; set; }
        public string Environment { get; set; } = null!;
        public string Issuer_Ruc { get; set; } = null!;
        public string Issuer_BusinessName { get; set; } = null!;
        public string? Issuer_CommercialName { get; set; }
        public string? Issuer_Address { get; set; }
        public string Receiver_IdType { get; set; } = null!;
        public string Receiver_Id { get; set; } = null!;
        public string Receiver_Name { get; set; } = null!;
        public string? Receiver_Email { get; set; }
        public string? Receiver_Phone { get; set; }
        public string? Receiver_Address { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Taxes { get; set; }
        public decimal Total { get; set; }
        public string OriginalXml { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    private sealed class LineRow
    {
        public Guid Id { get; set; }
        public Guid ElectronicDocumentId { get; set; }
        public string? Code { get; set; }
        public string Description { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Subtotal { get; set; }
    }

    private sealed class EmailLogRow
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

    private sealed class TaxBucketRow
    {
        public string CodigoPorcentaje { get; set; } = null!;
        public decimal BaseImponible { get; set; }
        public decimal Valor { get; set; }
    }
#pragma warning restore IDE1006
}
