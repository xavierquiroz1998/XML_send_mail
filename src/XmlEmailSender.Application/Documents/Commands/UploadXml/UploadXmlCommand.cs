using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Application.Documents.Dtos;

namespace XmlEmailSender.Application.Documents.Commands.UploadXml;

/// <summary>
/// Sube y persiste un XML autorizado del SRI. Idempotente por clave de acceso:
/// si ya existe, devuelve el documento existente sin duplicar.
/// </summary>
public sealed record UploadXmlCommand(string XmlContent) : ICommand<DocumentDto>;
