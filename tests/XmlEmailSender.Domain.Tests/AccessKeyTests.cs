using FluentAssertions;
using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Domain.Tests;

public class AccessKeyTests
{
    [Fact]
    public void Create_WithEmptyValue_ShouldFail()
    {
        var result = AccessKey.Create("");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AccessKey.Empty");
    }

    [Fact]
    public void Create_WithNullValue_ShouldFail()
    {
        var result = AccessKey.Create(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AccessKey.Empty");
    }

    [Fact]
    public void Create_WithWrongLength_ShouldFail()
    {
        var result = AccessKey.Create("12345");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AccessKey.Length");
    }

    [Fact]
    public void Create_WithNonDigits_ShouldFail()
    {
        var key = new string('A', 49);
        var result = AccessKey.Create(key);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AccessKey.NotNumeric");
    }

    [Fact]
    public void Create_WithInvalidCheckDigit_ShouldFail()
    {
        // 49 dígitos, dígito verificador deliberadamente incorrecto.
        var key = new string('1', 48) + "0";
        var result = AccessKey.Create(key);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AccessKey.CheckDigit");
    }

    [Fact]
    public void FromTrustedSource_BypassesValidation()
    {
        var ak = AccessKey.FromTrustedSource("anything-from-database");
        ak.Value.Should().Be("anything-from-database");
    }
}
