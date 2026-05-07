using AutoMapper;
using MediatR;
using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Application.Documents.Dtos;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Documents.Queries.ListDocuments;

public sealed record ListDocumentsQuery(int Skip = 0, int Take = 50) : IQuery<IReadOnlyList<DocumentDto>>;

internal sealed class ListDocumentsQueryHandler
    : IRequestHandler<ListDocumentsQuery, Result<IReadOnlyList<DocumentDto>>>
{
    private readonly IElectronicDocumentRepository _repo;
    private readonly IMapper _mapper;

    public ListDocumentsQueryHandler(IElectronicDocumentRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<DocumentDto>>> Handle(
        ListDocumentsQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.Take, 1, 200);
        var skip = Math.Max(request.Skip, 0);
        var items = await _repo.ListAsync(skip, take, ct);
        return Result.Success<IReadOnlyList<DocumentDto>>(
            _mapper.Map<List<DocumentDto>>(items));
    }
}
