using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XmlEmailSender.Application.Abstractions.Parsing;
using XmlEmailSender.Domain.Repositories;
using XmlEmailSender.Infrastructure.Parsing;
using XmlEmailSender.Infrastructure.Parsing.Strategies;
using XmlEmailSender.Infrastructure.Persistence;
using XmlEmailSender.Infrastructure.Persistence.Repositories;
using XmlEmailSender.Infrastructure.Persistence.Schema;

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

        return services;
    }
}
