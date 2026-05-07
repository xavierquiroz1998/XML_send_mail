using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Application.Abstractions.Pdf;

/// <summary>
/// Contrato público para generar el RIDE (Representación Impresa del
/// Documento Electrónico) en PDF a partir de un comprobante ya parseado.
/// </summary>
public interface IRideGenerator
{
    /// <summary>
    /// Genera el PDF del RIDE y devuelve los bytes.
    /// La implementación es responsable de elegir la plantilla correcta
    /// según <see cref="ElectronicDocument.Type"/>.
    /// </summary>
    byte[] Generate(ElectronicDocument document);
}
