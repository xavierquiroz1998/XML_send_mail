using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Application.Smtp.Dtos;

namespace XmlEmailSender.Application.Smtp.Commands.SaveSmtpConfiguration;

/// <summary>
/// Crea o actualiza una configuración SMTP. Si Id es null, crea; si tiene
/// valor, actualiza. La password viene en claro y se cifra antes de persistir.
/// Si NewPassword es null al actualizar, se conserva la password existente.
/// </summary>
public sealed record SaveSmtpConfigurationCommand(
    Guid? Id,
    string Name,
    string Host,
    int Port,
    bool UseSsl,
    string Username,
    string? NewPassword,
    string FromEmail,
    string FromName,
    bool Activate) : ICommand<SmtpConfigurationDto>;
