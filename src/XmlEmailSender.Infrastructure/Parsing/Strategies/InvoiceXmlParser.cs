using System.Xml.Linq;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Documents;
using static XmlEmailSender.Infrastructure.Parsing.XmlParsingHelpers;

namespace XmlEmailSender.Infrastructure.Parsing.Strategies;

internal sealed class InvoiceXmlParser : IDocumentTypeParser
{
    public string RootName => "factura";
    public DocumentType DocumentType => DocumentType.Invoice;

    public Result<ElectronicDocument> Parse(XDocument doc, string originalXml)
    {
        var root = doc.Root!;
        var infoTributaria = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("infoTributaria", StringComparison.OrdinalIgnoreCase));
        var infoFactura = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("infoFactura", StringComparison.OrdinalIgnoreCase));

        if (infoTributaria == null)
            return Result.Failure<ElectronicDocument>(
                Error.Validation("Xml.MissingInfoTributaria", "Falta el nodo <infoTributaria>."));
        if (infoFactura == null)
            return Result.Failure<ElectronicDocument>(
                Error.Validation("Xml.MissingInfoFactura", "Falta el nodo <infoFactura>."));

        var accessKeyResult = AccessKey.Create(GetElementValue(infoTributaria, "claveAcceso"));
        if (accessKeyResult.IsFailure)
            return Result.Failure<ElectronicDocument>(accessKeyResult.Error);

        var ambiente = GetElementValue(infoTributaria, "ambiente") == "1" ? "PRUEBAS" : "PRODUCCION";

        var issuer = new Issuer(
            Ruc: GetElementValue(infoTributaria, "ruc"),
            BusinessName: GetElementValue(infoTributaria, "razonSocial"),
            CommercialName: GetElementValue(infoTributaria, "nombreComercial"),
            Address: GetElementValue(infoTributaria, "dirMatriz"));

        var receiverEmail = FindCampoAdicional(root, "email", "correo", "e-mail", "mail");
        var receiverPhone = FindCampoAdicional(root, "telefono", "celular");
        var receiverAddress = FindCampoAdicional(root, "direccion", "dirección");

        var receiver = new Receiver(
            IdentificationType: GetElementValue(infoFactura, "tipoIdentificacionComprador"),
            Identification: GetElementValue(infoFactura, "identificacionComprador"),
            Name: GetElementValue(infoFactura, "razonSocialComprador"),
            Email: receiverEmail,
            Phone: receiverPhone,
            Address: receiverAddress);

        var subtotal = ParseDecimalSafe(GetElementValue(infoFactura, "totalSinImpuestos"));
        var total = ParseDecimalSafe(GetElementValue(infoFactura, "importeTotal"));

        var taxes = 0m;
        var totalConImpuestos = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("totalConImpuestos", StringComparison.OrdinalIgnoreCase));
        if (totalConImpuestos != null)
        {
            taxes = totalConImpuestos.Descendants()
                .Where(e => e.Name.LocalName.Equals("totalImpuesto", StringComparison.OrdinalIgnoreCase))
                .Sum(t => ParseDecimalSafe(GetElementValue(t, "valor")));
        }

        var lines = new List<DocumentLine>();
        var detalles = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("detalles", StringComparison.OrdinalIgnoreCase));
        if (detalles != null)
        {
            foreach (var det in detalles.Descendants()
                .Where(e => e.Name.LocalName.Equals("detalle", StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add(new DocumentLine(
                    code: GetElementValue(det, "codigoPrincipal"),
                    description: GetElementValue(det, "descripcion"),
                    quantity: ParseDecimalSafe(GetElementValue(det, "cantidad")),
                    unitPrice: ParseDecimalSafe(GetElementValue(det, "precioUnitario")),
                    discount: ParseDecimalSafe(GetElementValue(det, "descuento")),
                    subtotal: ParseDecimalSafe(GetElementValue(det, "precioTotalSinImpuesto"))));
            }
        }

        var document = new ElectronicDocument(
            type: DocumentType,
            accessKey: accessKeyResult.Value,
            documentNumber: ExtractDocumentNumber(infoTributaria),
            issueDate: ParseSriDate(GetElementValue(infoFactura, "fechaEmision")),
            environment: ambiente,
            issuer: issuer,
            receiver: receiver,
            subtotal: subtotal,
            taxes: taxes,
            total: total,
            originalXml: originalXml,
            lines: lines);

        return Result.Success(document);
    }
}
