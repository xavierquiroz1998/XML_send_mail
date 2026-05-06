using XmlEmailSender.Application.Abstractions.Parsing;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Infrastructure.Parsing.Strategies;

namespace XmlEmailSender.Infrastructure.Parsing;

public sealed class SriXmlDocumentParser : IXmlDocumentParser
{
    private readonly Dictionary<string, IDocumentTypeParser> _parsers;

    internal SriXmlDocumentParser(IEnumerable<IDocumentTypeParser> parsers)
    {
        _parsers = parsers.ToDictionary(
            p => p.RootName.ToLowerInvariant(),
            p => p);
    }

    /// <summary>
    /// Crea una instancia con los parsers por defecto (Invoice, CreditNote, Withholding).
    /// Útil para tests sin contenedor DI.
    /// </summary>
    public static SriXmlDocumentParser CreateDefault()
        => new(new IDocumentTypeParser[]
        {
            new InvoiceXmlParser(),
            new CreditNoteXmlParser(),
            new WithholdingXmlParser()
        });

    public Result<ElectronicDocument> Parse(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return Result.Failure<ElectronicDocument>(
                Error.Validation("Xml.Empty", "El XML está vacío."));

        try
        {
            var doc = XmlExtractor.ExtractDocumentXml(xmlContent);
            var rootName = XmlExtractor.DetectDocumentRootName(doc).ToLowerInvariant();

            if (!_parsers.TryGetValue(rootName, out var parser))
                return Result.Failure<ElectronicDocument>(
                    Error.Validation("Xml.UnsupportedType",
                        $"Tipo de comprobante no soportado: <{rootName}>."));

            return parser.Parse(doc, xmlContent);
        }
        catch (System.Xml.XmlException ex)
        {
            return Result.Failure<ElectronicDocument>(
                Error.Validation("Xml.Malformed", $"XML malformado: {ex.Message}"));
        }
        catch (InvalidDataException ex)
        {
            return Result.Failure<ElectronicDocument>(
                Error.Validation("Xml.Invalid", ex.Message));
        }
    }
}
