using Microsoft.Extensions.Options;
using XmlEmailSender.Domain.Documents;

namespace XmlEmailSender.Infrastructure.Pdf.Qr;

internal sealed class SriUrlBuilder : ISriUrlBuilder
{
    private readonly SriUrlOptions _options;

    public SriUrlBuilder(IOptions<SriUrlOptions> options) => _options = options.Value;

    public string BuildConsultUrl(ElectronicDocument document)
    {
        var baseUrl = document.Environment.Equals("PRUEBAS", StringComparison.OrdinalIgnoreCase)
            ? _options.TestingConsultUrl
            : _options.ProductionConsultUrl;
        return baseUrl + document.AccessKey.Value;
    }
}
