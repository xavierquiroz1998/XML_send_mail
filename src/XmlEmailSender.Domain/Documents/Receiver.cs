namespace XmlEmailSender.Domain.Documents;

public sealed record Receiver(
    string IdentificationType,
    string Identification,
    string Name,
    string? Email,
    string? Phone,
    string? Address);
