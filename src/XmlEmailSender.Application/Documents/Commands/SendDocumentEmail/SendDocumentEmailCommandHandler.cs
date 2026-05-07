using System.Text;
using AutoMapper;
using MediatR;
using XmlEmailSender.Application.Abstractions.Email;
using XmlEmailSender.Application.Abstractions.Pdf;
using XmlEmailSender.Application.Common.Errors;
using XmlEmailSender.Application.Emails.Dtos;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Emails;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Documents.Commands.SendDocumentEmail;

internal sealed class SendDocumentEmailCommandHandler
    : IRequestHandler<SendDocumentEmailCommand, Result<EmailLogDto>>
{
    private readonly IElectronicDocumentRepository _docs;
    private readonly IEmailLogRepository _logs;
    private readonly IRideGenerator _ride;
    private readonly IEmailSender _email;
    private readonly ISmtpCredentialsProvider _credentials;
    private readonly IMapper _mapper;

    public SendDocumentEmailCommandHandler(
        IElectronicDocumentRepository docs,
        IEmailLogRepository logs,
        IRideGenerator ride,
        IEmailSender email,
        ISmtpCredentialsProvider credentials,
        IMapper mapper)
    {
        _docs = docs;
        _logs = logs;
        _ride = ride;
        _email = email;
        _credentials = credentials;
        _mapper = mapper;
    }

    public async Task<Result<EmailLogDto>> Handle(SendDocumentEmailCommand request, CancellationToken ct)
    {
        var doc = await _docs.GetByIdAsync(request.DocumentId, ct);
        if (doc is null)
            return Result.Failure<EmailLogDto>(ApplicationErrors.Document.NotFound);

        var recipient = request.RecipientOverride ?? doc.Receiver.Email;
        if (string.IsNullOrWhiteSpace(recipient))
            return Result.Failure<EmailLogDto>(ApplicationErrors.Email.NoRecipient);

        // Validamos que haya SMTP activo antes de generar el PDF (operación cara).
        var credsResult = await _credentials.GetActiveAsync(ct);
        if (credsResult.IsFailure)
            return Result.Failure<EmailLogDto>(credsResult.Error);

        var subject = $"{doc.Type} {doc.DocumentNumber} — {doc.Issuer.BusinessName}";
        var log = new EmailLog(doc.Id, recipient, subject);
        await _logs.AddAsync(log, ct);

        // Generamos RIDE + adjuntamos XML original.
        var pdf = _ride.Generate(doc);
        var xml = Encoding.UTF8.GetBytes(doc.OriginalXml);

        var safeNumber = doc.DocumentNumber.Replace("/", "-").Replace(" ", "_");
        var attachments = new List<EmailAttachment>
        {
            new($"{safeNumber}.xml", "application/xml", xml),
            new($"{safeNumber}.pdf", "application/pdf", pdf),
        };

        var html = BuildBody(doc.Issuer.BusinessName, doc.DocumentNumber, doc.Total);

        var sendResult = await _email.SendAsync(
            new EmailMessage(recipient, doc.Receiver.Name, subject, html, attachments), ct);

        if (sendResult.IsSuccess)
        {
            log.MarkAsSent();
            await _logs.UpdateAsync(log, ct);
            return Result.Success(_mapper.Map<EmailLogDto>(log));
        }

        // Marcamos el log como fallido pero NO devolvemos Failure: el log
        // se persiste igual (UoW commitea) y devolvemos al caller el log
        // con status=Failed para que la UI lo muestre.
        log.MarkAsFailed(sendResult.Error.Message);
        await _logs.UpdateAsync(log, ct);
        return Result.Failure<EmailLogDto>(sendResult.Error);
    }

    private static string BuildBody(string issuerName, string number, decimal total) => $@"
<html>
  <body style=""font-family: system-ui, sans-serif; color: #111;"">
    <p>Estimado/a cliente,</p>
    <p>Adjuntamos su comprobante <strong>{number}</strong> emitido por <strong>{issuerName}</strong>
       por un total de <strong>USD {total:0.00}</strong>.</p>
    <p>Encontrará el archivo XML autorizado por el SRI y la representación impresa (RIDE) en formato PDF.</p>
    <p style=""color: #555; font-size: 12px;"">Mensaje generado por XmlEmailSender.</p>
  </body>
</html>";
}
