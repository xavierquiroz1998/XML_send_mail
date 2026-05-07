using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Infrastructure.Pdf.Qr;

public interface ISriUrlBuilder
{
    /// <summary>
    /// Construye la URL pública del SRI para consultar el comprobante,
    /// usando la URL base correcta según el ambiente del documento.
    /// </summary>
    string BuildConsultUrl(ElectronicDocument document);
}
