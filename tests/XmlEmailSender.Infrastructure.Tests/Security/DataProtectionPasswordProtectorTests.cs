using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using XmlEmailSender.Infrastructure.Security;

namespace XmlEmailSender.Infrastructure.Tests.Security;

public class DataProtectionPasswordProtectorTests
{
    private readonly DataProtectionPasswordProtector _protector;

    public DataProtectionPasswordProtectorTests()
    {
        // Ephemeral provider: las claves viven solo en memoria del test.
        _protector = new DataProtectionPasswordProtector(new EphemeralDataProtectionProvider());
    }

    [Fact]
    public void Protect_Then_Unprotect_RoundTripsPlainText()
    {
        var original = "S3cret-SMTP-Password!";
        var cipher = _protector.Protect(original);
        cipher.Should().NotBeNullOrEmpty().And.NotBe(original);

        var roundTrip = _protector.Unprotect(cipher);
        roundTrip.Should().Be(original);
    }

    [Fact]
    public void Protect_TwoCalls_ProduceDifferentCiphers()
    {
        // DataProtection incluye un IV/timestamp aleatorio, así que el mismo
        // texto cifrado dos veces produce blobs distintos. Eso es correcto:
        // evita correlación de cifrados en bases comprometidas.
        var a = _protector.Protect("same");
        var b = _protector.Protect("same");
        a.Should().NotBe(b);
        _protector.Unprotect(a).Should().Be(_protector.Unprotect(b));
    }
}
