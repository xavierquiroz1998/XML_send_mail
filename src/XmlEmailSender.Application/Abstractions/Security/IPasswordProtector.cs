namespace XmlEmailSender.Application.Abstractions.Security;

/// <summary>
/// Cifrado simétrico para credenciales que deben almacenarse en la base
/// (típicamente passwords SMTP). La implementación se basa en
/// IDataProtectionProvider de ASP.NET — la clave maestra la gestiona el
/// framework.
/// </summary>
public interface IPasswordProtector
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
}
