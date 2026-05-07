using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Infrastructure.Pdf.Qr;

namespace XmlEmailSender.Infrastructure.Pdf.Templates;

internal sealed class WithholdingRideTemplate : IRideTemplate
{
    private readonly ISriUrlBuilder _urlBuilder;

    public WithholdingRideTemplate(ISriUrlBuilder urlBuilder) => _urlBuilder = urlBuilder;

    public DocumentType DocumentType => DocumentType.WithholdingReceipt;

    public void Compose(IContainer container, ElectronicDocument document, byte[] qrPng)
    {
        var sriUrl = _urlBuilder.BuildConsultUrl(document);

        container.Column(col =>
        {
            col.Spacing(RideStyles.SectionSpacing);
            col.Item().Element(c => RideSections.ComposeHeader(c, document, "COMPROBANTE DE RETENCIÓN"));
            col.Item().Element(c => RideSections.ComposeReceiver(c, document));

            // Tabla de impuestos retenidos (un row por bucket = código de retención).
            col.Item().Border(0.5f).BorderColor(RideStyles.BorderColor).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);    // código de retención
                    c.RelativeColumn(2);    // base imponible
                    c.RelativeColumn(2);    // valor retenido
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeaderCell).Text("Código de retención");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Base imponible");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Valor retenido");
                });

                if (document.TaxBreakdown.Count == 0)
                {
                    // Backwards compat: documento viejo sin breakdown persistido.
                    table.Cell().Element(BodyCell).Text("—").FontSize(RideStyles.SmallFontSize);
                    table.Cell().Element(BodyCell).AlignRight().Text("—").FontSize(RideStyles.SmallFontSize);
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(document.Taxes.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                }
                else
                {
                    foreach (var bucket in document.TaxBreakdown)
                    {
                        table.Cell().Element(BodyCell)
                            .Text(bucket.CodigoPorcentaje).FontSize(RideStyles.SmallFontSize);
                        table.Cell().Element(BodyCell).AlignRight()
                            .Text(bucket.BaseImponible.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                        table.Cell().Element(BodyCell).AlignRight()
                            .Text(bucket.Valor.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                    }
                }

                static IContainer HeaderCell(IContainer c) => c
                    .Background(Colors.Grey.Lighten3).Padding(RideStyles.CellPadding)
                    .DefaultTextStyle(t => t.FontSize(RideStyles.SmallFontSize).Bold());
                static IContainer BodyCell(IContainer c) => c
                    .BorderTop(0.5f).BorderColor(RideStyles.BorderColor).Padding(RideStyles.CellPadding);
            });

            col.Item().AlignRight().Width(220).Column(totals =>
            {
                totals.Item().BorderTop(0.5f).BorderColor(RideStyles.BorderColor).Row(r =>
                {
                    r.RelativeItem().AlignRight().Text("TOTAL RETENIDO")
                        .FontSize(RideStyles.BodyFontSize).Bold();
                    r.ConstantItem(80).AlignRight()
                        .Text(document.Total.ToString("0.00"))
                        .FontSize(RideStyles.BodyFontSize).Bold();
                });
            });

            col.Item().PaddingTop(8).Element(c => RideSections.ComposeFooter(c, qrPng, sriUrl));
        });
    }
}
