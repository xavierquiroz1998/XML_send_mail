using AutoMapper;
using XmlEmailSender.Application.Documents.Dtos;
using XmlEmailSender.Application.Emails.Dtos;
using XmlEmailSender.Application.Smtp.Dtos;
using XmlEmailSender.Domain.Documents;
using XmlEmailSender.Domain.Emails;

namespace XmlEmailSender.Application.Common.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DocumentLine, DocumentLineDto>();

        CreateMap<TaxBucket, TaxBucketDto>()
            .ConstructUsing(b => new TaxBucketDto(
                b.CodigoPorcentaje,
                IvaCodeLabels.Resolve(b.CodigoPorcentaje),
                b.BaseImponible,
                b.Valor));

        CreateMap<ElectronicDocument, DocumentDto>()
            .ConstructUsing((d, ctx) => new DocumentDto(
                d.Id,
                (int)d.Type,
                d.Type.ToString(),
                d.AccessKey.Value,
                d.DocumentNumber,
                d.IssueDate,
                d.Environment,
                d.Issuer.Ruc,
                d.Issuer.BusinessName,
                d.Receiver.Identification,
                d.Receiver.Name,
                d.Receiver.Email,
                d.Subtotal,
                d.Taxes,
                d.Total,
                ctx.Mapper.Map<List<DocumentLineDto>>(d.Lines),
                ctx.Mapper.Map<List<TaxBucketDto>>(d.TaxBreakdown)));

        CreateMap<EmailLog, EmailLogDto>()
            .ConstructUsing(e => new EmailLogDto(
                e.Id,
                e.ElectronicDocumentId,
                e.RecipientEmail,
                e.Subject,
                (int)e.Status,
                e.Status.ToString(),
                e.ErrorMessage,
                e.SentAt,
                e.CreatedAt));

        CreateMap<SmtpConfiguration, SmtpConfigurationDto>()
            .ConstructUsing(s => new SmtpConfigurationDto(
                s.Id, s.Name, s.Host, s.Port, s.UseSsl, s.Username,
                s.FromEmail, s.FromName, s.IsActive, s.CreatedAt, s.UpdatedAt));
    }
}
