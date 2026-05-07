using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Application.Abstractions.Email;

public interface IEmailSender
{
    /// <summary>
    /// Envía un correo con N adjuntos. Devuelve Result.Success si el SMTP aceptó
    /// el mensaje, o Result.Failure con el mensaje de error original.
    /// La implementación NO debe lanzar excepciones por fallos de SMTP esperables
    /// (timeouts, auth, server-rejected) — debe retornar Failure.
    /// </summary>
    Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string ToEmail,
    string ToName,
    string Subject,
    string HtmlBody,
    IReadOnlyList<EmailAttachment> Attachments);

public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content);
