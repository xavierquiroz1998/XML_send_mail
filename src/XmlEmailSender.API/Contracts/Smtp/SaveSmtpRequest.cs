namespace XmlEmailSender.API.Contracts.Smtp;

/// <summary>
/// Body unificado para POST y PUT de SMTP. En POST el password es obligatorio;
/// en PUT es opcional (null = mantener el actual). El controller decide
/// si crea o actualiza según la presencia de Id en la URL.
/// </summary>
public sealed record SaveSmtpRequest(
    string Name,
    string Host,
    int Port,
    bool UseSsl,
    string Username,
    string? Password,
    string FromEmail,
    string FromName,
    bool Activate);
