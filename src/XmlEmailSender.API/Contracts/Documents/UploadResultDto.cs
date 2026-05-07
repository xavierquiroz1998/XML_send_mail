using XmlEmailSender.Application.Documents.Dtos;

namespace XmlEmailSender.API.Contracts.Documents;

/// <summary>
/// Resultado por archivo del endpoint POST /api/documents/upload.
/// Permite reportar éxitos y errores en una sola respuesta cuando el cliente
/// sube varios XMLs.
/// </summary>
public sealed record UploadResultDto(
    string FileName,
    bool Success,
    DocumentDto? Document,
    string? ErrorCode,
    string? ErrorMessage);
