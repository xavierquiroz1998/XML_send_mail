using System.Xml.Linq;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Infrastructure.Parsing.Strategies;

internal interface IDocumentTypeParser
{
    string RootName { get; }
    DocumentType DocumentType { get; }
    Result<ElectronicDocument> Parse(XDocument doc, string originalXml);
}
