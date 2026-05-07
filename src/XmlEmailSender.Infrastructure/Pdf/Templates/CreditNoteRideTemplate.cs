using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Infrastructure.Pdf.Qr;

namespace XmlEmailSender.Infrastructure.Pdf.Templates;

internal sealed class CreditNoteRideTemplate : IRideTemplate
{
    private readonly ISriUrlBuilder _urlBuilder;

    public CreditNoteRideTemplate(ISriUrlBuilder urlBuilder) => _urlBuilder = urlBuilder;

    public DocumentType DocumentType => DocumentType.CreditNote;

    public void Compose(IContainer container, ElectronicDocument document, byte[] qrPng)
    {
        var sriUrl = _urlBuilder.BuildConsultUrl(document);

        container.Column(col =>
        {
            col.Spacing(RideStyles.SectionSpacing);
            col.Item().Element(c => RideSections.ComposeHeader(c, document, "NOTA DE CRÉDITO"));
            col.Item().Element(c => RideSections.ComposeReceiver(c, document));

            // Detalle (mismo layout que factura)
            col.Item().Border(0.5f).BorderColor(RideStyles.BorderColor).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(50);
                    c.RelativeColumn(4);
                    c.ConstantColumn(40);
                    c.ConstantColumn(55);
                    c.ConstantColumn(45);
                    c.ConstantColumn(60);
                });
                table.Header(h =>
                {
                    h.Cell().Element(HeaderCell).Text("Cód.");
                    h.Cell().Element(HeaderCell).Text("Descripción");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Cant.");
                    h.Cell().Element(HeaderCell).AlignRight().Text("P. Unit.");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Desc.");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Total");
                });
                foreach (var line in document.Lines)
                {
                    table.Cell().Element(BodyCell).Text(line.Code).FontSize(RideStyles.SmallFontSize);
                    table.Cell().Element(BodyCell).Text(line.Description).FontSize(RideStyles.SmallFontSize);
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(line.Quantity.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(line.UnitPrice.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(line.Discount.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(line.Subtotal.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                }

                static IContainer HeaderCell(IContainer c) => c
                    .Background(Colors.Grey.Lighten3).Padding(RideStyles.CellPadding)
                    .DefaultTextStyle(t => t.FontSize(RideStyles.SmallFontSize).Bold());
                static IContainer BodyCell(IContainer c) => c
                    .BorderTop(0.5f).BorderColor(RideStyles.BorderColor).Padding(RideStyles.CellPadding);
            });

            // Totales (con bucket dinámico)
            col.Item().AlignRight().Width(220).Column(totals =>
            {
                totals.Item().Row(r =>
                {
                    r.RelativeItem().AlignRight().Text("Subtotal").FontSize(RideStyles.SmallFontSize);
                    r.ConstantItem(80).AlignRight()
                        .Text(document.Subtotal.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                });
                foreach (var bucket in document.TaxBreakdown)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().AlignRight()
                            .Text(IvaCodeLabels.Resolve(bucket.CodigoPorcentaje))
                            .FontSize(RideStyles.SmallFontSize);
                        r.ConstantItem(80).AlignRight()
                            .Text(bucket.Valor.ToString("0.00")).FontSize(RideStyles.SmallFontSize);
                    });
                }
                totals.Item().PaddingTop(2).BorderTop(0.5f).BorderColor(RideStyles.BorderColor)
                    .Row(r =>
                    {
                        r.RelativeItem().AlignRight().Text("VALOR MODIFICACIÓN")
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
