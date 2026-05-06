using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Domain.Emails;

public sealed class SmtpConfiguration : Entity
{
    public string Name { get; private set; } = null!;
    public string Host { get; private set; } = null!;
    public int Port { get; private set; }
    public bool UseSsl { get; private set; }
    public string Username { get; private set; } = null!;
    public string EncryptedPassword { get; private set; } = null!;
    public string FromEmail { get; private set; } = null!;
    public string FromName { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private SmtpConfiguration() { }

    public SmtpConfiguration(
        string name,
        string host,
        int port,
        bool useSsl,
        string username,
        string encryptedPassword,
        string fromEmail,
        string fromName)
    {
        Name = name;
        Host = host;
        Port = port;
        UseSsl = useSsl;
        Username = username;
        EncryptedPassword = encryptedPassword;
        FromEmail = fromEmail;
        FromName = fromName;
        IsActive = true;
    }

    public void Update(
        string name,
        string host,
        int port,
        bool useSsl,
        string username,
        string? encryptedPassword,
        string fromEmail,
        string fromName)
    {
        Name = name;
        Host = host;
        Port = port;
        UseSsl = useSsl;
        Username = username;
        if (!string.IsNullOrEmpty(encryptedPassword))
            EncryptedPassword = encryptedPassword;
        FromEmail = fromEmail;
        FromName = fromName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    public static SmtpConfiguration Hydrate(
        Guid id,
        string name,
        string host,
        int port,
        bool useSsl,
        string username,
        string encryptedPassword,
        string fromEmail,
        string fromName,
        bool isActive,
        DateTime createdAt,
        DateTime? updatedAt)
        => new()
        {
            Id = id,
            Name = name,
            Host = host,
            Port = port,
            UseSsl = useSsl,
            Username = username,
            EncryptedPassword = encryptedPassword,
            FromEmail = fromEmail,
            FromName = fromName,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}
