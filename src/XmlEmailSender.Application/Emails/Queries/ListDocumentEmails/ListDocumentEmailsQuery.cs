using AutoMapper;
using MediatR;
using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Application.Emails.Dtos;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Emails.Queries.ListDocumentEmails;

public sealed record ListDocumentEmailsQuery(Guid DocumentId) : IQuery<IReadOnlyList<EmailLogDto>>;

internal sealed class ListDocumentEmailsQueryHandler
    : IRequestHandler<ListDocumentEmailsQuery, Result<IReadOnlyList<EmailLogDto>>>
{
    private readonly IEmailLogRepository _logs;
    private readonly IMapper _mapper;

    public ListDocumentEmailsQueryHandler(IEmailLogRepository logs, IMapper mapper)
    {
        _logs = logs;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EmailLogDto>>> Handle(
        ListDocumentEmailsQuery request, CancellationToken ct)
    {
        var items = await _logs.ListByDocumentAsync(request.DocumentId, ct);
        return Result.Success<IReadOnlyList<EmailLogDto>>(
            _mapper.Map<List<EmailLogDto>>(items));
    }
}
