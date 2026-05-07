using QuestPDF.Helpers;

namespace XmlEmailSender.Infrastructure.Pdf.Templates;

/// <summary>
/// Constantes de estilo para todos los RIDE. Centralizar evita inconsistencias
/// entre Factura, Nota de Crédito y Retención.
/// </summary>
internal static class RideStyles
{
    public const float TitleFontSize = 12;
    public const float HeadingFontSize = 9;
    public const float BodyFontSize = 8;
    public const float SmallFontSize = 7;

    public static readonly string BorderColor = Colors.Grey.Lighten1;
    public static readonly string LabelColor = Colors.Grey.Darken2;
    public static readonly string AccentColor = Colors.Blue.Darken3;

    public const float SectionSpacing = 6f;
    public const float CellPadding = 4f;
}
