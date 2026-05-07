using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using XmlEmailSender.Application.Abstractions.Email;
using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Infrastructure.Email;

internal sealed class MailKitEmailSender : IEmailSender
{
    private readonly ISmtpCredentialsProvider _credentials;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(
        ISmtpCredentialsProvider credentials,
        ILogger<MailKitEmailSender> logger)
    {
        _credentials = credentials;
        _logger = logger;
    }

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var credsResult = await _credentials.GetActiveAsync(ct);
        if (credsResult.IsFailure) return Result.Failure(credsResult.Error);
        var creds = credsResult.Value;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(creds.FromName, creds.FromEmail));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder { HtmlBody = message.HtmlBody };
        foreach (var att in message.Attachments)
            builder.Attachments.Add(att.FileName, att.Content,
                ContentType.Parse(att.ContentType));
        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            // STARTTLS si UseSsl=true en puerto 587; SslOnConnect si 465.
            var secure = creds.UseSsl
                ? (creds.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                : SecureSocketOptions.None;

            await client.ConnectAsync(creds.Host, creds.Port, secure, ct);
            await client.AuthenticateAsync(creds.Username, creds.Password, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(quit: true, ct);

            _logger.LogInformation("SMTP send OK -> {Recipient} ({Subject})",
                message.ToEmail, message.Subject);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP send FAIL -> {Recipient}: {Reason}",
                message.ToEmail, ex.Message);
            return Result.Failure(Error.Failure("Smtp.SendFailed", ex.Message));
        }
    }
}
