using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Infrastructure.Parsing;
using XmlEmailSender.Infrastructure.Persistence;
using XmlEmailSender.Infrastructure.Persistence.Repositories;
using XmlEmailSender.Infrastructure.Persistence.Schema;
using XmlEmailSender.Infrastructure.Tests.Parsing;

namespace XmlEmailSender.Infrastructure.Tests.Persistence;

/// <summary>
/// Integración real contra SQLite en archivo temporal:
/// schema runner aplica scripts → repo inserta → repo lee → roundtrip OK.
/// </summary>
public class ElectronicDocumentRepositoryTests : IAsyncLifetime
{
    private string _dbPath = null!;
    private DbConnectionFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"xes_test_{Guid.NewGuid():N}.db");
        _factory = new DbConnectionFactory(DatabaseProvider.Sqlite, $"Data Source={_dbPath}");
        var runner = new SchemaMigrationRunner(_factory, NullLogger<SchemaMigrationRunner>.Instance);
        await runner.RunAsync();
    }

    public Task DisposeAsync()
    {
        try { File.Delete(_dbPath); } catch { /* ignore */ }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddAndGetByAccessKey_RoundTrip()
    {
        var parser = SriXmlDocumentParser.CreateDefault();
        var xml = SampleXml.MinimalInvoice(SampleXml.ValidAccessKey);
        var parsed = parser.Parse(xml);
        parsed.IsSuccess.Should().BeTrue();

        await using var uow = new UnitOfWork(_factory);
        await uow.BeginAsync();
        var repo = new ElectronicDocumentRepository(uow);

        await repo.AddAsync(parsed.Value);
        await uow.CommitAsync();

        // Nueva UoW para lectura "limpia"
        await using var readUow = new UnitOfWork(_factory);
        var readRepo = new ElectronicDocumentRepository(readUow);

        var found = await readRepo.GetByAccessKeyAsync(SampleXml.ValidAccessKey);

        found.Should().NotBeNull();
        found!.Type.Should().Be(DocumentType.Invoice);
        found.DocumentNumber.Should().Be("001-001-000000123");
        found.Issuer.Ruc.Should().Be("1790012345001");
        found.Receiver.Email.Should().Be("cliente@ejemplo.com");
        found.Total.Should().Be(112m);
        found.Lines.Should().HaveCount(1);
        found.Lines.First().Description.Should().Be("Producto de prueba");
    }

    [Fact]
    public async Task ExistsByAccessKey_ReturnsTrueAfterInsert()
    {
        var parser = SriXmlDocumentParser.CreateDefault();
        var parsed = parser.Parse(SampleXml.MinimalInvoice(SampleXml.ValidAccessKey));

        await using var uow = new UnitOfWork(_factory);
        await uow.BeginAsync();
        var repo = new ElectronicDocumentRepository(uow);
        await repo.AddAsync(parsed.Value);
        await uow.CommitAsync();

        await using var readUow = new UnitOfWork(_factory);
        var readRepo = new ElectronicDocumentRepository(readUow);
        var exists = await readRepo.ExistsByAccessKeyAsync(SampleXml.ValidAccessKey);
        exists.Should().BeTrue();
    }
}
