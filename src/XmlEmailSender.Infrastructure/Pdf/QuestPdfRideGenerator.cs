using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using XmlEmailSender.Application.Abstractions.Pdf;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Infrastructure.Pdf.Qr;
using XmlEmailSender.Infrastructure.Pdf.Templates;

namespace XmlEmailSender.Infrastructure.Pdf;

internal sealed class QuestPdfRideGenerator : IRideGenerator
{
    private readonly RideTemplateFactory _factory;
    private readonly IQrCodeGenerator _qr;
    private readonly ISriUrlBuilder _urlBuilder;

    public QuestPdfRideGenerator(
        RideTemplateFactory factory,
        IQrCodeGenerator qr,
        ISriUrlBuilder urlBuilder)
    {
        _factory = factory;
        _qr = qr;
        _urlBuilder = urlBuilder;
    }

    /// <summary>
    /// Helper para tests / scripts: arma todo el pipeline RIDE (factory + plantillas
    /// + QR + URL builder con opciones por defecto) sin contenedor DI.
    /// </summary>
    public static QuestPdfRideGenerator CreateDefault()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var urlOptions = Microsoft.Extensions.Options.Options.Create(new SriUrlOptions());
        var urlBuilder = new SriUrlBuilder(urlOptions);
        var qr = new ZxingQrCodeGenerator();
        var templates = new IRideTemplate[]
        {
            new InvoiceRideTemplate(urlBuilder),
            new CreditNoteRideTemplate(urlBuilder),
            new WithholdingRideTemplate(urlBuilder)
        };
        return new QuestPdfRideGenerator(new RideTemplateFactory(templates), qr, urlBuilder);
    }

    public byte[] Generate(ElectronicDocument document)
    {
        var template = _factory.For(document.Type);

        // El QR codifica la URL pública del SRI (consulta del comprobante por clave de acceso).
        var url = _urlBuilder.BuildConsultUrl(document);
        var qrPng = _qr.Generate(url, sizePx: 220);

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(t => t.FontFamily(Fonts.Lato));
                page.Content().Element(c => template.Compose(c, document, qrPng));
            });
        }).GeneratePdf();
    }
}
