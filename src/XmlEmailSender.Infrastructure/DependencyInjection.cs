using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF;
using QuestPDF.Infrastructure;
using XmlEmailSender.Application.Abstractions.Email;
using XmlEmailSender.Application.Abstractions.Parsing;
using XmlEmailSender.Application.Abstractions.Pdf;
using XmlEmailSender.Application.Abstractions.Security;
using XmlEmailSender.Domain.Repositories;
using XmlEmailSender.Infrastructure.Email;
using XmlEmailSender.Infrastructure.Parsing;
using XmlEmailSender.Infrastructure.Parsing.Strategies;
using XmlEmailSender.Infrastructure.Pdf;
using XmlEmailSender.Infrastructure.Pdf.Qr;
using XmlEmailSender.Infrastructure.Pdf.Templates;
using XmlEmailSender.Infrastructure.Persistence;
using XmlEmailSender.Infrastructure.Persistence.Repositories;
using XmlEmailSender.Infrastructure.Persistence.Schema;
using XmlEmailSender.Infrastructure.Security;

namespace XmlEmailSender.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var providerName = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionString 'Default' no configurado.");

        var provider = providerName.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? DatabaseProvider.SqlServer
            : DatabaseProvider.Sqlite;

        // El DbConnectionFactory registra internamente los Dapper type handlers cuando es SQLite.
        services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(provider, connectionString));
        services.AddSingleton<SchemaMigrationRunner>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IElectronicDocumentRepository, ElectronicDocumentRepository>();
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();
        services.AddScoped<ISmtpConfigurationRepository, SmtpConfigurationRepository>();

        // Parsers SRI
        services.AddSingleton<IDocumentTypeParser, InvoiceXmlParser>();
        services.AddSingleton<IDocumentTypeParser, CreditNoteXmlParser>();
        services.AddSingleton<IDocumentTypeParser, WithholdingXmlParser>();
        services.AddSingleton<IXmlDocumentParser>(sp =>
            new SriXmlDocumentParser(sp.GetServices<IDocumentTypeParser>()));

        // PDF / RIDE
        // QuestPDF requiere declarar el tipo de licencia antes del primer render.
        // Community es válida si la facturación anual del usuario es < 1M USD.
        Settings.License = LicenseType.Community;

        services.Configure<SriUrlOptions>(configuration.GetSection(SriUrlOptions.SectionName));
        services.AddSingleton<ISriUrlBuilder, SriUrlBuilder>();
        services.AddSingleton<IQrCodeGenerator, ZxingQrCodeGenerator>();
        services.AddSingleton<IRideTemplate, InvoiceRideTemplate>();
        services.AddSingleton<IRideTemplate, CreditNoteRideTemplate>();
        services.AddSingleton<IRideTemplate, WithholdingRideTemplate>();
        services.AddSingleton<RideTemplateFactory>();
        services.AddSingleton<IRideGenerator, QuestPdfRideGenerator>();

        // Seguridad / cifrado SMTP
        // DataProtection persiste la clave maestra en disco; en Linux se ubica
        // en ~/.aspnet/DataProtection-Keys de forma automática. Forzamos un
        // ApplicationName para que keys generadas en distintos hosts del mismo
        // proyecto sean compatibles si se mueve la base.
        services.AddDataProtection().SetApplicationName("XmlEmailSender");
        services.AddSingleton<IPasswordProtector, DataProtectionPasswordProtector>();

        // Email
        services.AddScoped<ISmtpCredentialsProvider, SmtpCredentialsProvider>();
        services.AddScoped<IEmailSender, MailKitEmailSender>();

        return services;
    }
}
