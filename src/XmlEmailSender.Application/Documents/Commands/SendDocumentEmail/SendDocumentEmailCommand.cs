using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Application.Emails.Dtos;

namespace XmlEmailSender.Application.Documents.Commands.SendDocumentEmail;

/// <summary>
/// Envía un comprobante (XML + RIDE) por correo al receptor. Si no se pasa
/// recipientOverride, se usa el correo del Receiver del XML.
/// </summary>
public sealed record SendDocumentEmailCommand(
    Guid DocumentId,
    string? RecipientOverride) : ICommand<EmailLogDto>;
