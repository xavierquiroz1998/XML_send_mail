namespace XmlEmailSender.Application.Smtp.Dtos;

/// <summary>
/// DTO de SMTP que NUNCA expone la contraseña (ni cifrada).
/// Para el flujo de "test" o "send" usamos la entidad de dominio internamente.
/// </summary>
public sealed record SmtpConfigurationDto(
    Guid Id,
    string Name,
    string Host,
    int Port,
    bool UseSsl,
    string Username,
    string FromEmail,
    string FromName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
