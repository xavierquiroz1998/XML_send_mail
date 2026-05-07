using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Application.Common.Errors;

/// <summary>
/// Errores específicos de la capa Application. Centralizar evita
/// repetir códigos/mensajes en handlers y facilita su revisión.
/// </summary>
public static class ApplicationErrors
{
    public static class Document
    {
        public static readonly Error AlreadyExists = Error.Conflict(
            "Document.AlreadyExists",
            "Ya existe un comprobante con esta clave de acceso.");

        public static readonly Error NotFound = Error.NotFound(
            "Document.NotFound",
            "No se encontró el comprobante solicitado.");
    }

    public static class Email
    {
        public static readonly Error NoSmtpConfigured = Error.Failure(
            "Email.NoSmtpConfigured",
            "No hay una configuración SMTP activa.");

        public static readonly Error NoRecipient = Error.Validation(
            "Email.NoRecipient",
            "El comprobante no tiene correo del receptor y no se especificó uno explícito.");
    }

    public static class Smtp
    {
        public static readonly Error NotFound = Error.NotFound(
            "Smtp.NotFound",
            "No se encontró la configuración SMTP solicitada.");
    }
}
