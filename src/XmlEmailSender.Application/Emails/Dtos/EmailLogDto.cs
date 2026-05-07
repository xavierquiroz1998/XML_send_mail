namespace XmlEmailSender.Application.Emails.Dtos;

public sealed record EmailLogDto(
    Guid Id,
    Guid ElectronicDocumentId,
    string RecipientEmail,
    string Subject,
    int Status,                 // 0=Pending, 1=Sent, 2=Failed
    string StatusName,
    string? ErrorMessage,
    DateTime? SentAt,
    DateTime CreatedAt);
