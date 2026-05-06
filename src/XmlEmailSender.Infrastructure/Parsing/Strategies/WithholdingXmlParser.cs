using System.Xml.Linq;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Documents;
using static XmlEmailSender.Infrastructure.Parsing.XmlParsingHelpers;

namespace XmlEmailSender.Infrastructure.Parsing.Strategies;

internal sealed class WithholdingXmlParser : IDocumentTypeParser
{
    public string RootName => "comprobanteRetencion";
    public DocumentType DocumentType => DocumentType.WithholdingReceipt;

    public Result<ElectronicDocument> Parse(XDocument doc, string originalXml)
    {
        var root = doc.Root!;
        var infoTributaria = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("infoTributaria", StringComparison.OrdinalIgnoreCase));
        // El SRI usa "infoCompRetencion" para versiones <= 1.0.0 e "infoCompRetencion" / "infoCompRetencion" según versión
        var infoComp = root.Descendants()
            .FirstOrDefault(e =>
                e.Name.LocalName.Equals("infoCompRetencion", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("infoCompretencion", StringComparison.OrdinalIgnoreCase));

        if (infoTributaria == null || infoComp == null)
            return Result.Failure<ElectronicDocument>(
                Error.Validation("Xml.MissingNodes", "Faltan nodos obligatorios en el comprobante de retención."));

        var accessKeyResult = AccessKey.Create(GetElementValue(infoTributaria, "claveAcceso"));
        if (accessKeyResult.IsFailure)
            return Result.Failure<ElectronicDocument>(accessKeyResult.Error);

        var ambiente = GetElementValue(infoTributaria, "ambiente") == "1" ? "PRUEBAS" : "PRODUCCION";

        var issuer = new Issuer(
            GetElementValue(infoTributaria, "ruc"),
            GetElementValue(infoTributaria, "razonSocial"),
            GetElementValue(infoTributaria, "nombreComercial"),
            GetElementValue(infoTributaria, "dirMatriz"));

        var receiver = new Receiver(
            IdentificationType: GetElementValue(infoComp, "tipoIdentificacionSujetoRetenido"),
            Identification: GetElementValue(infoComp, "identificacionSujetoRetenido"),
            Name: GetElementValue(infoComp, "razonSocialSujetoRetenido"),
            Email: FindCampoAdicional(root, "email", "correo", "mail", "e-mail"),
            Phone: FindCampoAdicional(root, "telefono", "celular"),
            Address: FindCampoAdicional(root, "direccion", "dirección"));

        // Las retenciones no tienen "subtotal/impuestos/total" como facturas:
        // se suma el valorRetenido de cada impuesto.
        var totalRetenido = 0m;
        var impuestos = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("impuestos", StringComparison.OrdinalIgnoreCase));
        if (impuestos != null)
        {
            totalRetenido = impuestos.Descendants()
                .Where(e => e.Name.LocalName.Equals("impuesto", StringComparison.OrdinalIgnoreCase))
                .Sum(i => ParseDecimalSafe(GetElementValue(i, "valorRetenido")));
        }

        return Result.Success(new ElectronicDocument(
            DocumentType, accessKeyResult.Value,
            ExtractDocumentNumber(infoTributaria),
            ParseSriDate(GetElementValue(infoComp, "fechaEmision")),
            ambiente, issuer, receiver,
            subtotal: 0m,
            taxes: totalRetenido,
            total: totalRetenido,
            originalXml,
            lines: Array.Empty<DocumentLine>()));
    }
}
