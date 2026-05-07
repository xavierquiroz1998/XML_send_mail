using XmlEmailSender.Application.Abstractions.Email;
using XmlEmailSender.Application.Abstractions.Security;
using XmlEmailSender.Application.Common.Errors;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Infrastructure.Email;

internal sealed class SmtpCredentialsProvider : ISmtpCredentialsProvider
{
    private readonly ISmtpConfigurationRepository _repo;
    private readonly IPasswordProtector _protector;

    public SmtpCredentialsProvider(
        ISmtpConfigurationRepository repo,
        IPasswordProtector protector)
    {
        _repo = repo;
        _protector = protector;
    }

    public async Task<Result<SmtpCredentials>> GetActiveAsync(CancellationToken ct = default)
    {
        var config = await _repo.GetActiveAsync(ct);
        if (config is null)
            return Result.Failure<SmtpCredentials>(ApplicationErrors.Email.NoSmtpConfigured);

        // Si el descifrado falla (clave maestra cambiada, registro corrupto), exponer
        // el error como Failure en lugar de propagar la excepción.
        try
        {
            var password = _protector.Unprotect(config.EncryptedPassword);
            return Result.Success(new SmtpCredentials(
                config.Host, config.Port, config.UseSsl,
                config.Username, password, config.FromEmail, config.FromName));
        }
        catch (Exception ex)
        {
            return Result.Failure<SmtpCredentials>(Error.Failure(
                "Smtp.DecryptFailed",
                $"No se pudo descifrar la contraseña SMTP: {ex.Message}"));
        }
    }
}
