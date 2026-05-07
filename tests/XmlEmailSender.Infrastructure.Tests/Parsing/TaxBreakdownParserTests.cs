using FluentAssertions;
using XmlEmailSender.Infrastructure.Parsing;

namespace XmlEmailSender.Infrastructure.Tests.Parsing;

public class TaxBreakdownParserTests
{
    private readonly SriXmlDocumentParser _parser = SriXmlDocumentParser.CreateDefault();

    [Fact]
    public void Parse_LegacyInvoice_PopulatesSingleBucketCode2()
    {
        var xml = SampleXml.MinimalInvoice(SampleXml.ValidAccessKey);
        var result = _parser.Parse(xml);

        result.IsSuccess.Should().BeTrue();
        result.Value.TaxBreakdown.Should().HaveCount(1);
        var bucket = result.Value.TaxBreakdown.First();
        bucket.CodigoPorcentaje.Should().Be("2");      // legacy 12%
        bucket.BaseImponible.Should().Be(100m);
        bucket.Valor.Should().Be(12m);
    }

    [Fact]
    public void Parse_MixedTaxesInvoice_PopulatesTwoBuckets()
    {
        var xml = SampleXml.MinimalInvoiceWithMixedTaxes(SampleXml.ValidAccessKey);
        var result = _parser.Parse(xml);

        result.IsSuccess.Should().BeTrue();
        result.Value.TaxBreakdown.Should().HaveCount(2);

        var zeroBucket = result.Value.TaxBreakdown.Single(b => b.CodigoPorcentaje == "0");
        zeroBucket.Valor.Should().Be(0m);
        zeroBucket.BaseImponible.Should().Be(100m);

        var fifteenBucket = result.Value.TaxBreakdown.Single(b => b.CodigoPorcentaje == "4");
        fifteenBucket.Valor.Should().Be(15m);
        fifteenBucket.BaseImponible.Should().Be(100m);

        result.Value.Taxes.Should().Be(15m);   // suma de buckets
    }
}
