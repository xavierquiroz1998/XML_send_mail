using FluentAssertions;
using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Domain.Tests;

public class IvaCodeLabelsTests
{
    [Theory]
    [InlineData("0", "IVA 0%")]
    [InlineData("2", "IVA 12%")]
    [InlineData("3", "IVA 14%")]
    [InlineData("4", "IVA 15%")]
    [InlineData("5", "IVA 5%")]
    [InlineData("6", "No Objeto IVA")]
    [InlineData("7", "Exento de IVA")]
    [InlineData("8", "IRBPNR")]
    public void Resolve_KnownCodes_ReturnsExpectedLabel(string code, string expected)
    {
        IvaCodeLabels.Resolve(code).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("99")]
    public void Resolve_UnknownOrEmpty_FallsBackToGenericIva(string? code)
    {
        // Nunca mentir: si no conocemos el porcentaje, no inventamos uno.
        IvaCodeLabels.Resolve(code).Should().Be("IVA");
    }
}
