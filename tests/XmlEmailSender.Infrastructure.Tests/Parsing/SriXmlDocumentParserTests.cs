using FluentAssertions;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Infrastructure.Parsing;

namespace XmlEmailSender.Infrastructure.Tests.Parsing;

public class SriXmlDocumentParserTests
{
    private readonly SriXmlDocumentParser _parser = SriXmlDocumentParser.CreateDefault();

    [Fact]
    public void Parse_EmptyXml_ReturnsValidationError()
    {
        var result = _parser.Parse("");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Xml.Empty");
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsMalformedError()
    {
        var result = _parser.Parse("<no-cierra>");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Xml.Malformed");
    }

    [Fact]
    public void Parse_UnknownRoot_ReturnsUnsupportedTypeError()
    {
        var result = _parser.Parse("<otroComprobante></otroComprobante>");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Xml.UnsupportedType");
    }

    [Fact]
    public void Parse_InvoiceWithInvalidAccessKey_ReturnsAccessKeyError()
    {
        var xml = SampleXml.MinimalInvoice(accessKey: "1234"); // longitud inválida
        var result = _parser.Parse(xml);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("AccessKey.");
    }

    [Fact]
    public void Parse_InvoiceWithCdataEnvelope_UnwrapsAndParses()
    {
        var inner = SampleXml.MinimalInvoice(accessKey: SampleXml.ValidAccessKey);
        var envelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<autorizacion>
  <estado>AUTORIZADO</estado>
  <comprobante><![CDATA[{inner}]]></comprobante>
</autorizacion>";

        var result = _parser.Parse(envelope);
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(DocumentType.Invoice);
        result.Value.AccessKey.Value.Should().Be(SampleXml.ValidAccessKey);
    }

    [Fact]
    public void Parse_InvoiceDirect_NoEnvelope_Works()
    {
        var xml = SampleXml.MinimalInvoice(accessKey: SampleXml.ValidAccessKey);
        var result = _parser.Parse(xml);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentNumber.Should().Be("001-001-000000123");
        result.Value.Issuer.Ruc.Should().Be("1790012345001");
        result.Value.Receiver.Email.Should().Be("cliente@ejemplo.com");
        result.Value.Receiver.Name.Should().Be("Juan Pérez");
        result.Value.Total.Should().Be(112m);
        result.Value.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_InvoiceWithoutEmail_LeavesReceiverEmailNull()
    {
        var xml = SampleXml.MinimalInvoice(accessKey: SampleXml.ValidAccessKey, includeEmail: false);
        var result = _parser.Parse(xml);

        result.IsSuccess.Should().BeTrue();
        result.Value.Receiver.Email.Should().BeNull();
    }
}
