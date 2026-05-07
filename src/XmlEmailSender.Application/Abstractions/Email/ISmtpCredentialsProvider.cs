using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Emails;

namespace XmlEmailSender.Application.Abstractions.Email;

/// <summary>
/// Devuelve la configuración SMTP activa con la password DESCIFRADA en memoria.
/// Implementado en Infrastructure (combina repo + IPasswordProtector).
/// </summary>
public interface ISmtpCredentialsProvider
{
    Task<Result<SmtpCredentials>> GetActiveAsync(CancellationToken ct = default);
}

/// <summary>
/// Snapshot que el IEmailSender consume directamente. La password está en
/// claro y solo vive en memoria mientras dura la operación de envío.
/// </summary>
public sealed record SmtpCredentials(
    string Host,
    int Port,
    bool UseSsl,
    string Username,
    string Password,
    string FromEmail,
    string FromName);
