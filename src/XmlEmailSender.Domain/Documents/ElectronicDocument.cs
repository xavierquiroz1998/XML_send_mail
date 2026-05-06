using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Emails;

namespace XmlEmailSender.Domain.Documents;

public sealed class ElectronicDocument : Entity
{
    public DocumentType Type { get; private set; }
    public AccessKey AccessKey { get; private set; } = null!;
    public string DocumentNumber { get; private set; } = null!;
    public DateTime IssueDate { get; private set; }
    public string Environment { get; private set; } = null!;

    public Issuer Issuer { get; private set; } = null!;
    public Receiver Receiver { get; private set; } = null!;

    public decimal Subtotal { get; private set; }
    public decimal Taxes { get; private set; }
    public decimal Total { get; private set; }

    public string OriginalXml { get; private set; } = null!;

    private readonly List<DocumentLine> _lines = new();
    public IReadOnlyCollection<DocumentLine> Lines => _lines.AsReadOnly();

    private readonly List<EmailLog> _emailLogs = new();
    public IReadOnlyCollection<EmailLog> EmailLogs => _emailLogs.AsReadOnly();

    private ElectronicDocument() { }

    public ElectronicDocument(
        DocumentType type,
        AccessKey accessKey,
        string documentNumber,
        DateTime issueDate,
        string environment,
        Issuer issuer,
        Receiver receiver,
        decimal subtotal,
        decimal taxes,
        decimal total,
        string originalXml,
        IEnumerable<DocumentLine> lines)
    {
        Type = type;
        AccessKey = accessKey;
        DocumentNumber = documentNumber;
        IssueDate = issueDate;
        Environment = environment;
        Issuer = issuer;
        Receiver = receiver;
        Subtotal = subtotal;
        Taxes = taxes;
        Total = total;
        OriginalXml = originalXml;
        foreach (var line in lines)
        {
            line.AttachTo(Id);
            _lines.Add(line);
        }
    }

    public void RegisterEmailAttempt(EmailLog log) => _emailLogs.Add(log);

    /// <summary>
    /// Reconstrucción desde la base de datos (Dapper).
    /// </summary>
    public static ElectronicDocument Hydrate(
        Guid id,
        DocumentType type,
        string accessKey,
        string documentNumber,
        DateTime issueDate,
        string environment,
        Issuer issuer,
        Receiver receiver,
        decimal subtotal,
        decimal taxes,
        decimal total,
        string originalXml,
        DateTime createdAt,
        DateTime? updatedAt,
        IEnumerable<DocumentLine> lines,
        IEnumerable<EmailLog> emailLogs)
    {
        var doc = new ElectronicDocument
        {
            Id = id,
            Type = type,
            AccessKey = AccessKey.FromTrustedSource(accessKey),
            DocumentNumber = documentNumber,
            IssueDate = issueDate,
            Environment = environment,
            Issuer = issuer,
            Receiver = receiver,
            Subtotal = subtotal,
            Taxes = taxes,
            Total = total,
            OriginalXml = originalXml,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        doc._lines.AddRange(lines);
        doc._emailLogs.AddRange(emailLogs);
        return doc;
    }
}
