using System.Text;
using FluentAssertions;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Infrastructure.Parsing;
using XmlEmailSender.Infrastructure.Pdf;
using XmlEmailSender.Infrastructure.Tests.Parsing;

namespace XmlEmailSender.Infrastructure.Tests.Pdf;

/// <summary>
/// Validamos: el pipeline completo (parser → factory → QR → QuestPDF → bytes)
/// produce un PDF válido. No comparamos visualmente; basta con cabecera %PDF-
/// y un peso plausible.
/// </summary>
public class QuestPdfRideGeneratorTests
{
    private readonly SriXmlDocumentParser _parser = SriXmlDocumentParser.CreateDefault();
    private readonly QuestPdfRideGenerator _generator = QuestPdfRideGenerator.CreateDefault();

    [Fact]
    public void Generate_Invoice_ReturnsValidPdfBytes()
    {
        var xml = SampleXml.MinimalInvoiceWithMixedTaxes(SampleXml.ValidAccessKey);
        var doc = _parser.Parse(xml).Value;

        var pdf = _generator.Generate(doc);

        AssertIsPdf(pdf);
        pdf.Length.Should().BeGreaterThan(2_000, "un RIDE con QR + tabla pesa varios KB");
    }

    [Fact]
    public void Generate_LegacyInvoice_NoBreakdownStillRenders()
    {
        // Documento materializado sin TaxBreakdown (escenario backwards-compat).
        var legacy = ElectronicDocument.Hydrate(
            id: Guid.NewGuid(),
            type: DocumentType.Invoice,
            accessKey: SampleXml.ValidAccessKey,
            documentNumber: "001-001-000000001",
            issueDate: new DateTime(2026, 5, 7),
            environment: "PRODUCCION",
            issuer: new Issuer("1790012345001", "EMPRESA X", "X", "Quito"),
            receiver: new Receiver("05", "1712345678", "Cliente X", "x@x.com", null, null),
            subtotal: 100m,
            taxes: 12m,
            total: 112m,
            originalXml: "<factura/>",
            createdAt: DateTime.UtcNow,
            updatedAt: null,
            lines: Array.Empty<DocumentLine>(),
            emailLogs: Array.Empty<XmlEmailSender.Domain.Emails.EmailLog>(),
            taxBreakdown: null);

        var pdf = _generator.Generate(legacy);
        AssertIsPdf(pdf);
    }

    [Fact]
    public void Generate_DifferentTypes_DispatchToDifferentTemplates()
    {
        // Invoice y CreditNote deben producir bytes distintos (títulos distintos
        // en el header). La forma más barata de validarlo sin imágenes es mirar
        // que los blobs no sean idénticos.
        var invoiceXml = SampleXml.MinimalInvoice(SampleXml.ValidAccessKey);
        var invoiceDoc = _parser.Parse(invoiceXml).Value;

        // Construimos manualmente una nota de crédito con los mismos datos
        // base para aislar la diferencia de plantilla, no de payload.
        var creditDoc = ElectronicDocument.Hydrate(
            id: Guid.NewGuid(),
            type: DocumentType.CreditNote,
            accessKey: invoiceDoc.AccessKey.Value,
            documentNumber: invoiceDoc.DocumentNumber,
            issueDate: invoiceDoc.IssueDate,
            environment: invoiceDoc.Environment,
            issuer: invoiceDoc.Issuer,
            receiver: invoiceDoc.Receiver,
            subtotal: invoiceDoc.Subtotal,
            taxes: invoiceDoc.Taxes,
            total: invoiceDoc.Total,
            originalXml: invoiceDoc.OriginalXml,
            createdAt: invoiceDoc.CreatedAt,
            updatedAt: null,
            lines: invoiceDoc.Lines,
            emailLogs: Array.Empty<XmlEmailSender.Domain.Emails.EmailLog>(),
            taxBreakdown: invoiceDoc.TaxBreakdown);

        var pdfInvoice = _generator.Generate(invoiceDoc);
        var pdfCredit = _generator.Generate(creditDoc);

        AssertIsPdf(pdfInvoice);
        AssertIsPdf(pdfCredit);
        pdfInvoice.SequenceEqual(pdfCredit).Should().BeFalse(
            "el header del PDF cambia entre 'FACTURA' y 'NOTA DE CRÉDITO'");
    }

    private static void AssertIsPdf(byte[] bytes)
    {
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(100);
        // Cabecera estándar PDF: "%PDF-".
        var header = Encoding.ASCII.GetString(bytes, 0, 5);
        header.Should().Be("%PDF-");
    }
}
