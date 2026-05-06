using System.Xml.Linq;

namespace XmlEmailSender.Infrastructure.Parsing;

internal static class XmlExtractor
{
    /// <summary>
    /// Devuelve el XDocument del comprobante real.
    /// Acepta tanto el sobre &lt;autorizacion&gt; con el comprobante como CDATA,
    /// como el comprobante (factura/notaCredito/comprobanteRetencion) directamente.
    /// </summary>
    public static XDocument ExtractDocumentXml(string rawXml)
    {
        var doc = XDocument.Parse(rawXml);
        var root = doc.Root ?? throw new InvalidDataException("XML sin nodo raíz.");

        if (root.Name.LocalName.Equals("autorizacion", StringComparison.OrdinalIgnoreCase) ||
            root.Name.LocalName.Equals("RespuestaAutorizacionComprobante", StringComparison.OrdinalIgnoreCase))
        {
            var comprobanteNode = root.Descendants()
                .FirstOrDefault(e => e.Name.LocalName.Equals("comprobante", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException("Sobre de autorización sin nodo <comprobante>.");

            var inner = comprobanteNode.Value;
            if (string.IsNullOrWhiteSpace(inner))
                throw new InvalidDataException("Nodo <comprobante> vacío.");

            return XDocument.Parse(inner);
        }

        return doc;
    }

    public static string DetectDocumentRootName(XDocument doc)
        => doc.Root?.Name.LocalName ?? string.Empty;
}
