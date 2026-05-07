using System.Xml.Linq;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Documents;
using static XmlEmailSender.Infrastructure.Parsing.XmlParsingHelpers;

namespace XmlEmailSender.Infrastructure.Parsing.Strategies;

internal sealed class CreditNoteXmlParser : IDocumentTypeParser
{
    public string RootName => "notaCredito";
    public DocumentType DocumentType => DocumentType.CreditNote;

    public Result<ElectronicDocument> Parse(XDocument doc, string originalXml)
    {
        var root = doc.Root!;
        var infoTributaria = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("infoTributaria", StringComparison.OrdinalIgnoreCase));
        var infoNota = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("infoNotaCredito", StringComparison.OrdinalIgnoreCase));

        if (infoTributaria == null || infoNota == null)
            return Result.Failure<ElectronicDocument>(
                Error.Validation("Xml.MissingNodes", "Faltan nodos obligatorios en la nota de crédito."));

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
            IdentificationType: GetElementValue(infoNota, "tipoIdentificacionComprador"),
            Identification: GetElementValue(infoNota, "identificacionComprador"),
            Name: GetElementValue(infoNota, "razonSocialComprador"),
            Email: FindCampoAdicional(root, "email", "correo", "mail", "e-mail"),
            Phone: FindCampoAdicional(root, "telefono", "celular"),
            Address: FindCampoAdicional(root, "direccion", "dirección"));

        var subtotal = ParseDecimalSafe(GetElementValue(infoNota, "totalSinImpuestos"));
        var total = ParseDecimalSafe(GetElementValue(infoNota, "valorModificacion"));

        var taxBreakdown = new List<TaxBucket>();
        var taxes = 0m;
        var tot = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("totalConImpuestos", StringComparison.OrdinalIgnoreCase));
        if (tot != null)
        {
            var buckets = tot.Descendants()
                .Where(e => e.Name.LocalName.Equals("totalImpuesto", StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => GetElementValue(t, "codigoPorcentaje"))
                .Select(g => new TaxBucket(
                    CodigoPorcentaje: g.Key,
                    BaseImponible: g.Sum(x => ParseDecimalSafe(GetElementValue(x, "baseImponible"))),
                    Valor: g.Sum(x => ParseDecimalSafe(GetElementValue(x, "valor")))));
            taxBreakdown.AddRange(buckets);
            taxes = taxBreakdown.Sum(b => b.Valor);
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
                    GetElementValue(det, "codigoInterno"),
                    GetElementValue(det, "descripcion"),
                    ParseDecimalSafe(GetElementValue(det, "cantidad")),
                    ParseDecimalSafe(GetElementValue(det, "precioUnitario")),
                    ParseDecimalSafe(GetElementValue(det, "descuento")),
                    ParseDecimalSafe(GetElementValue(det, "precioTotalSinImpuesto"))));
            }
        }

        return Result.Success(new ElectronicDocument(
            DocumentType, accessKeyResult.Value,
            ExtractDocumentNumber(infoTributaria),
            ParseSriDate(GetElementValue(infoNota, "fechaEmision")),
            ambiente, issuer, receiver, subtotal, taxes, total, originalXml, lines,
            taxBreakdown: taxBreakdown));
    }
}
