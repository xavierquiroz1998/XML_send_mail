using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Application.Abstractions.Parsing;

public interface IXmlDocumentParser
{
    Result<ElectronicDocument> Parse(string xmlContent);
}
