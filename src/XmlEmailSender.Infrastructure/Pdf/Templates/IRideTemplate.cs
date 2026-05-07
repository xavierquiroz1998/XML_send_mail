using QuestPDF.Infrastructure;
using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Infrastructure.Pdf.Templates;

/// <summary>
/// Contrato interno: cada tipo de comprobante implementa una plantilla.
/// El generador la inserta dentro de un IDocument de QuestPDF.
/// </summary>
internal interface IRideTemplate
{
    DocumentType DocumentType { get; }

    /// <summary>
    /// Compone el cuerpo del PDF. Recibe el contexto de QuestPDF, el documento
    /// a renderizar y los bytes del QR ya generado.
    /// </summary>
    void Compose(IContainer container, ElectronicDocument document, byte[] qrPng);
}
