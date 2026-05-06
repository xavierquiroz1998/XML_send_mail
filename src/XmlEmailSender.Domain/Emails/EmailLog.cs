using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Domain.Emails;

public sealed class EmailLog : Entity
{
    public Guid ElectronicDocumentId { get; private set; }
    public string RecipientEmail { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public EmailStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? SentAt { get; private set; }

    private EmailLog() { }

    public EmailLog(Guid documentId, string recipientEmail, string subject)
    {
        ElectronicDocumentId = documentId;
        RecipientEmail = recipientEmail;
        Subject = subject;
        Status = EmailStatus.Pending;
    }

    public void MarkAsSent()
    {
        Status = EmailStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = EmailStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public static EmailLog Hydrate(
        Guid id,
        Guid electronicDocumentId,
        string recipientEmail,
        string subject,
        EmailStatus status,
        string? errorMessage,
        DateTime? sentAt,
        DateTime createdAt,
        DateTime? updatedAt)
        => new()
        {
            Id = id,
            ElectronicDocumentId = electronicDocumentId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Status = status,
            ErrorMessage = errorMessage,
            SentAt = sentAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}
