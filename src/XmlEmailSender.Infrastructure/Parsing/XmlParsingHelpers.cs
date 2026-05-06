using System.Globalization;
using System.Xml.Linq;

namespace XmlEmailSender.Infrastructure.Parsing;

internal static class XmlParsingHelpers
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static string GetElementValue(XElement parent, string elementName)
        => parent.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase))
            ?.Value.Trim() ?? string.Empty;

    public static string? GetElementValueOrNull(XElement parent, string elementName)
    {
        var v = GetElementValue(parent, elementName);
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    public static decimal ParseDecimalSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        var normalized = value.Trim().Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Number, InvariantCulture, out var result)
            ? result
            : 0m;
    }

    public static DateTime ParseSriDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DateTime.MinValue;
        string[] formats = { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
        return DateTime.TryParseExact(value.Trim(), formats, InvariantCulture,
            DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MinValue;
    }

    /// <summary>
    /// Busca un &lt;campoAdicional nombre="..."&gt; tolerando variaciones de mayúsculas
    /// y nombres alternativos para el mismo concepto (ej. "email" / "correo" / "mail").
    /// </summary>
    public static string? FindCampoAdicional(XElement root, params string[] possibleNames)
    {
        var infoAdicional = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals("infoAdicional", StringComparison.OrdinalIgnoreCase));

        if (infoAdicional == null) return null;

        foreach (var campo in infoAdicional.Descendants()
            .Where(e => e.Name.LocalName.Equals("campoAdicional", StringComparison.OrdinalIgnoreCase)))
        {
            var nombreAttr = campo.Attribute("nombre")?.Value
                          ?? campo.Attribute("Nombre")?.Value
                          ?? campo.Attribute("NOMBRE")?.Value;

            if (nombreAttr == null) continue;

            if (possibleNames.Any(n => nombreAttr.Equals(n, StringComparison.OrdinalIgnoreCase)))
            {
                var v = campo.Value.Trim();
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
        }

        return null;
    }

    public static string ExtractDocumentNumber(XElement infoTributaria)
    {
        var estab = GetElementValue(infoTributaria, "estab");
        var ptoEmi = GetElementValue(infoTributaria, "ptoEmi");
        var secuencial = GetElementValue(infoTributaria, "secuencial");
        return $"{estab}-{ptoEmi}-{secuencial}";
    }
}
