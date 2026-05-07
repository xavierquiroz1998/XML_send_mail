using Microsoft.AspNetCore.DataProtection;
using XmlEmailSender.Application.Abstractions.Security;

namespace XmlEmailSender.Infrastructure.Security;

/// <summary>
/// Implementación del IPasswordProtector basada en IDataProtectionProvider de
/// ASP.NET. La clave maestra la genera/persiste el framework (en Linux,
/// ~/.aspnet/DataProtection-Keys; en Windows, registro DPAPI).
/// </summary>
internal sealed class DataProtectionPasswordProtector : IPasswordProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionPasswordProtector(IDataProtectionProvider provider)
    {
        // Purpose string: aísla cifrados destinados a passwords SMTP de otros usos.
        _protector = provider.CreateProtector("XmlEmailSender.Smtp.Password.v1");
    }

    public string Protect(string plainText) => _protector.Protect(plainText);
    public string Unprotect(string cipherText) => _protector.Unprotect(cipherText);
}
