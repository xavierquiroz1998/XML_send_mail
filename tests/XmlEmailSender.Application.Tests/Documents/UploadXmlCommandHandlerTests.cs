using AutoMapper;
using FluentAssertions;
using Moq;
using XmlEmailSender.Application.Abstractions.Parsing;
using XmlEmailSender.Application.Common.Mapping;
using XmlEmailSender.Application.Documents.Commands.UploadXml;
using XmlEmailSender.Application.Documents.Dtos;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Tests.Documents;

public class UploadXmlCommandHandlerTests
{
    private readonly IMapper _mapper;

    public UploadXmlCommandHandlerTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task Handle_ParserFailure_ReturnsParserError()
    {
        var parser = new Mock<IXmlDocumentParser>();
        parser.Setup(p => p.Parse(It.IsAny<string>()))
              .Returns(Result.Failure<ElectronicDocument>(Error.Validation("Xml.Empty", "vacío")));

        var repo = new Mock<IElectronicDocumentRepository>();

        var sut = new UploadXmlCommandHandler(parser.Object, repo.Object, _mapper);

        var result = await sut.Handle(new UploadXmlCommand("anything"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Xml.Empty");
        repo.Verify(r => r.AddAsync(It.IsAny<ElectronicDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingDocument_ReturnsExistingWithoutInsert()
    {
        var doc = BuildDocument();

        var parser = new Mock<IXmlDocumentParser>();
        parser.Setup(p => p.Parse(It.IsAny<string>())).Returns(Result.Success(doc));

        var repo = new Mock<IElectronicDocumentRepository>();
        repo.Setup(r => r.GetByAccessKeyAsync(doc.AccessKey.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var sut = new UploadXmlCommandHandler(parser.Object, repo.Object, _mapper);

        var result = await sut.Handle(new UploadXmlCommand("xml"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessKey.Should().Be(doc.AccessKey.Value);
        repo.Verify(r => r.AddAsync(It.IsAny<ElectronicDocument>(), It.IsAny<CancellationToken>()),
            Times.Never, "no debe duplicar");
    }

    [Fact]
    public async Task Handle_NewDocument_PersistsAndReturnsDto()
    {
        var doc = BuildDocument();

        var parser = new Mock<IXmlDocumentParser>();
        parser.Setup(p => p.Parse(It.IsAny<string>())).Returns(Result.Success(doc));

        var repo = new Mock<IElectronicDocumentRepository>();
        repo.Setup(r => r.GetByAccessKeyAsync(doc.AccessKey.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ElectronicDocument?)null);

        var sut = new UploadXmlCommandHandler(parser.Object, repo.Object, _mapper);

        var result = await sut.Handle(new UploadXmlCommand("xml"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<DocumentDto>();
        repo.Verify(r => r.AddAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ElectronicDocument BuildDocument()
    {
        // Construimos un AccessKey real (mod-11 válido) usando el mismo helper
        // que los tests del parser, pero como esto es Application.Tests no
        // dependemos de Infrastructure: lo hacemos con FromTrustedSource.
        var accessKey = AccessKey.FromTrustedSource(new string('1', 49));
        return new ElectronicDocument(
            DocumentType.Invoice, accessKey,
            "001-001-000000001",
            new DateTime(2026, 5, 7),
            "PRODUCCION",
            new Issuer("1790012345001", "X", "X", "Quito"),
            new Receiver("05", "1712345678", "Cliente", "c@x.com", null, null),
            subtotal: 100m, taxes: 15m, total: 115m,
            originalXml: "<factura/>",
            lines: Array.Empty<DocumentLine>(),
            taxBreakdown: new[] { new TaxBucket("4", 100m, 15m) });
    }
}
