using AutoMapper;
using FluentAssertions;
using Moq;
using XmlEmailSender.Application.Abstractions.Email;
using XmlEmailSender.Application.Abstractions.Pdf;
using XmlEmailSender.Application.Common.Mapping;
using XmlEmailSender.Application.Documents.Commands.SendDocumentEmail;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Domain.Emails;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Tests.Documents;

public class SendDocumentEmailCommandHandlerTests
{
    private readonly IMapper _mapper;

    public SendDocumentEmailCommandHandlerTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ReturnsNotFound()
    {
        var (sut, mocks) = BuildSut();
        mocks.Docs.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ElectronicDocument?)null);

        var result = await sut.Handle(
            new SendDocumentEmailCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");
    }

    [Fact]
    public async Task Handle_NoEmail_NoOverride_ReturnsValidationError()
    {
        var doc = BuildDocument(withReceiverEmail: false);
        var (sut, mocks) = BuildSut();
        mocks.Docs.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);

        var result = await sut.Handle(
            new SendDocumentEmailCommand(doc.Id, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.NoRecipient");
    }

    [Fact]
    public async Task Handle_NoSmtp_ReturnsConfiguredError()
    {
        var doc = BuildDocument();
        var (sut, mocks) = BuildSut();
        mocks.Docs.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);
        mocks.Credentials.Setup(c => c.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SmtpCredentials>(
                Error.Failure("Email.NoSmtpConfigured", "no smtp")));

        var result = await sut.Handle(
            new SendDocumentEmailCommand(doc.Id, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.NoSmtpConfigured");
        mocks.Email.Verify(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmailSentOk_LogsAsSent_AndReturnsDto()
    {
        var doc = BuildDocument();
        var (sut, mocks) = BuildSut();

        mocks.Docs.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);
        mocks.Credentials.Setup(c => c.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SmtpCredentials(
                "smtp", 587, true, "u", "p", "noreply@x.com", "X")));
        mocks.Ride.Setup(r => r.Generate(doc)).Returns(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        mocks.Email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        EmailLog? capturedLog = null;
        mocks.Logs.Setup(l => l.AddAsync(It.IsAny<EmailLog>(), It.IsAny<CancellationToken>()))
            .Callback<EmailLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        var result = await sut.Handle(
            new SendDocumentEmailCommand(doc.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be((int)EmailStatus.Sent);
        result.Value.SentAt.Should().NotBeNull();
        capturedLog.Should().NotBeNull();
        mocks.Logs.Verify(l => l.UpdateAsync(It.IsAny<EmailLog>(), It.IsAny<CancellationToken>()),
            Times.Once, "el log se actualiza con el resultado del envío");
    }

    [Fact]
    public async Task Handle_EmailSendFails_LogsAsFailed_AndReturnsFailure()
    {
        var doc = BuildDocument();
        var (sut, mocks) = BuildSut();

        mocks.Docs.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doc);
        mocks.Credentials.Setup(c => c.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SmtpCredentials(
                "smtp", 587, true, "u", "p", "noreply@x.com", "X")));
        mocks.Ride.Setup(r => r.Generate(doc)).Returns(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        mocks.Email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Failure("Smtp.SendFailed", "timeout")));

        EmailLog? capturedLog = null;
        mocks.Logs.Setup(l => l.AddAsync(It.IsAny<EmailLog>(), It.IsAny<CancellationToken>()))
            .Callback<EmailLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        var result = await sut.Handle(
            new SendDocumentEmailCommand(doc.Id, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Smtp.SendFailed");
        capturedLog!.Status.Should().Be(EmailStatus.Failed);
        capturedLog.ErrorMessage.Should().Be("timeout");
    }

    private (SendDocumentEmailCommandHandler Sut,
             (Mock<IElectronicDocumentRepository> Docs,
              Mock<IEmailLogRepository> Logs,
              Mock<IRideGenerator> Ride,
              Mock<IEmailSender> Email,
              Mock<ISmtpCredentialsProvider> Credentials) Mocks) BuildSut()
    {
        var docs = new Mock<IElectronicDocumentRepository>();
        var logs = new Mock<IEmailLogRepository>();
        var ride = new Mock<IRideGenerator>();
        var email = new Mock<IEmailSender>();
        var creds = new Mock<ISmtpCredentialsProvider>();

        var handler = new SendDocumentEmailCommandHandler(
            docs.Object, logs.Object, ride.Object, email.Object, creds.Object, _mapper);

        return (handler, (docs, logs, ride, email, creds));
    }

    private static ElectronicDocument BuildDocument(bool withReceiverEmail = true)
    {
        var ak = AccessKey.FromTrustedSource(new string('1', 49));
        return new ElectronicDocument(
            DocumentType.Invoice, ak,
            "001-001-000000123",
            new DateTime(2026, 5, 7),
            "PRODUCCION",
            new Issuer("1790012345001", "EMPRESA X", "X", "Quito"),
            new Receiver("05", "1712345678", "Cliente",
                Email: withReceiverEmail ? "c@x.com" : null,
                Phone: null, Address: null),
            subtotal: 100m, taxes: 15m, total: 115m,
            originalXml: "<factura/>",
            lines: Array.Empty<DocumentLine>(),
            taxBreakdown: new[] { new TaxBucket("4", 100m, 15m) });
    }
}
