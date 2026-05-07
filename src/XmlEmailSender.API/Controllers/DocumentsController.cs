using MediatR;
using Microsoft.AspNetCore.Mvc;
using XmlEmailSender.API.Common;
using XmlEmailSender.API.Contracts.Documents;
using XmlEmailSender.Application.Documents.Commands.SendDocumentEmail;
using XmlEmailSender.Application.Documents.Commands.UploadXml;
using XmlEmailSender.Application.Documents.Queries.GetDocumentById;
using XmlEmailSender.Application.Documents.Queries.GetRidePdf;
using XmlEmailSender.Application.Documents.Queries.ListDocuments;
using XmlEmailSender.Application.Emails.Queries.ListDocumentEmails;

namespace XmlEmailSender.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Sube uno o varios XML autorizados del SRI. Devuelve un resultado por archivo.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]  // 50 MB total
    public async Task<ActionResult<IReadOnlyList<UploadResultDto>>> Upload(
        [FromForm] List<IFormFile> files,
        CancellationToken ct)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "No se recibió ningún archivo." });

        var results = new List<UploadResultDto>(files.Count);
        foreach (var file in files)
        {
            string content;
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                content = await reader.ReadToEndAsync(ct);
            }
            catch (Exception ex)
            {
                results.Add(new UploadResultDto(
                    file.FileName, false, null, "Upload.ReadFailed", ex.Message));
                continue;
            }

            var cmd = new UploadXmlCommand(content);
            var result = await _mediator.Send(cmd, ct);

            results.Add(result.IsSuccess
                ? new UploadResultDto(file.FileName, true, result.Value, null, null)
                : new UploadResultDto(file.FileName, false, null, result.Error.Code, result.Error.Message));
        }

        return Ok(results);
    }

    /// <summary>Lista documentos persistidos, paginados.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListDocumentsQuery(skip, take), ct);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un documento por su Id (incluye líneas, breakdown y emails).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDocumentByIdQuery(id), ct);
        return result.ToActionResult();
    }

    /// <summary>Genera el PDF del RIDE al vuelo y lo descarga.</summary>
    [HttpGet("{id:guid}/ride")]
    public async Task<IActionResult> GetRide(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRidePdfQuery(id), ct);
        if (result.IsFailure) return result.ToActionResult();
        var pdf = result.Value;
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    /// <summary>Envía el documento por correo al receptor (opcionalmente override).</summary>
    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(
        Guid id,
        [FromBody] SendEmailRequest? request,
        CancellationToken ct)
    {
        var cmd = new SendDocumentEmailCommand(id, request?.RecipientOverride);
        var result = await _mediator.Send(cmd, ct);
        return result.ToActionResult();
    }

    /// <summary>Historial de envíos del documento.</summary>
    [HttpGet("{id:guid}/emails")]
    public async Task<IActionResult> ListEmails(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListDocumentEmailsQuery(id), ct);
        return result.ToActionResult();
    }
}
