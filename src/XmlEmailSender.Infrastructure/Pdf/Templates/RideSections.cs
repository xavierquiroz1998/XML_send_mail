using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Infrastructure.Pdf.Templates;

/// <summary>
/// Secciones compartidas entre las tres plantillas (Factura, NotaCrédito, Retención).
/// </summary>
internal static class RideSections
{
    /// <summary>
    /// Header en dos columnas: datos del emisor (izquierda) + datos de autorización
    /// y clave de acceso (derecha).
    /// </summary>
    public static void ComposeHeader(IContainer container, ElectronicDocument document, string title)
    {
        container.Row(row =>
        {
            // Columna izquierda: emisor
            row.RelativeItem(2).Border(0.5f).BorderColor(RideStyles.BorderColor)
                .Padding(RideStyles.CellPadding)
                .Column(col =>
                {
                    col.Item().Text(document.Issuer.BusinessName)
                        .FontSize(RideStyles.TitleFontSize).Bold();
                    if (!string.IsNullOrWhiteSpace(document.Issuer.CommercialName))
                        col.Item().Text(document.Issuer.CommercialName).FontSize(RideStyles.BodyFontSize);
                    if (!string.IsNullOrWhiteSpace(document.Issuer.Address))
                        col.Item().Text($"Dirección Matriz: {document.Issuer.Address}")
                            .FontSize(RideStyles.SmallFontSize);
                    col.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("Obligado a llevar Contabilidad: ").FontSize(RideStyles.SmallFontSize);
                        t.Span("SI").FontSize(RideStyles.SmallFontSize).Bold();
                    });
                });

            row.ConstantItem(8); // gutter

            // Columna derecha: autorización + clave de acceso
            row.RelativeItem(2).Border(0.5f).BorderColor(RideStyles.BorderColor)
                .Padding(RideStyles.CellPadding)
                .Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        t.Span("R.U.C.: ").FontSize(RideStyles.SmallFontSize).SemiBold();
                        t.Span(document.Issuer.Ruc).FontSize(RideStyles.BodyFontSize);
                    });
                    col.Item().Text(title)
                        .FontSize(RideStyles.TitleFontSize).Bold().FontColor(RideStyles.AccentColor);
                    col.Item().Text(t =>
                    {
                        t.Span("Nº ").FontSize(RideStyles.SmallFontSize).SemiBold();
                        t.Span(document.DocumentNumber).FontSize(RideStyles.BodyFontSize).Bold();
                    });
                    col.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("NÚMERO DE AUTORIZACIÓN").FontSize(RideStyles.SmallFontSize).SemiBold();
                    });
                    col.Item().Text(document.AccessKey.Value).FontSize(RideStyles.SmallFontSize);
                    col.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span("FECHA Y HORA AUT.: ").FontSize(RideStyles.SmallFontSize).SemiBold();
                        t.Span(document.IssueDate.ToString("dd/MM/yyyy")).FontSize(RideStyles.SmallFontSize);
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("AMBIENTE: ").FontSize(RideStyles.SmallFontSize).SemiBold();
                        t.Span(document.Environment).FontSize(RideStyles.SmallFontSize);
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("EMISIÓN: ").FontSize(RideStyles.SmallFontSize).SemiBold();
                        t.Span("NORMAL").FontSize(RideStyles.SmallFontSize);
                    });
                    col.Item().PaddingTop(2).Text("CLAVE DE ACCESO:")
                        .FontSize(RideStyles.SmallFontSize).SemiBold();
                    col.Item().Text(document.AccessKey.Value).FontSize(RideStyles.SmallFontSize);
                });
        });
    }

    /// <summary>
    /// Bloque del receptor (cliente / sujeto retenido).
    /// </summary>
    public static void ComposeReceiver(IContainer container, ElectronicDocument document)
    {
        container.Border(0.5f).BorderColor(RideStyles.BorderColor)
            .Padding(RideStyles.CellPadding)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Razón Social / Nombres y Apellidos: ")
                            .FontSize(RideStyles.SmallFontSize).SemiBold();
                        t.Span(document.Receiver.Name).FontSize(RideStyles.BodyFontSize);
                    });
                    row.ConstantItem(120).Text(t =>
                    {
                        t.Span("Identificación: ").FontSize(RideStyles.SmallFontSize).SemiBold();
                        t.Span(document.Receiver.Identification).FontSize(RideStyles.BodyFontSize);
                    });
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Fecha de emisión: ").FontSize(RideStyles.SmallFontSize).SemiBold();
                        t.Span(document.IssueDate.ToString("dd/MM/yyyy")).FontSize(RideStyles.BodyFontSize);
                    });
                    if (!string.IsNullOrWhiteSpace(document.Receiver.Email))
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Correo: ").FontSize(RideStyles.SmallFontSize).SemiBold();
                            t.Span(document.Receiver.Email!).FontSize(RideStyles.BodyFontSize);
                        });
                    }
                });
            });
    }

    /// <summary>
    /// Pie con la URL del SRI y el QR para verificación móvil.
    /// </summary>
    public static void ComposeFooter(IContainer container, byte[] qrPng, string sriUrl)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignBottom().Column(col =>
            {
                col.Item().Text("Verificación SRI:")
                    .FontSize(RideStyles.SmallFontSize).SemiBold();
                col.Item().Text(sriUrl).FontSize(RideStyles.SmallFontSize)
                    .FontColor(Colors.Blue.Medium);
                col.Item().PaddingTop(4).Text(
                    "Documento generado por XmlEmailSender — verifique siempre el QR con la app del SRI.")
                    .FontSize(RideStyles.SmallFontSize).Italic();
            });
            row.ConstantItem(110).Image(qrPng);
        });
    }
}
