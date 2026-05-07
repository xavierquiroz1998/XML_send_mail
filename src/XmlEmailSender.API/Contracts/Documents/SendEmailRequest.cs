namespace XmlEmailSender.API.Contracts.Documents;

/// <summary>
/// Body opcional para POST /api/documents/{id}/send.
/// Si <see cref="RecipientOverride"/> viene null, el handler usa el correo
/// del receptor del XML.
/// </summary>
public sealed record SendEmailRequest(string? RecipientOverride);
