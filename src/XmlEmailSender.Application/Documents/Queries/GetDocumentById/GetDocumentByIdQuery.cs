using AutoMapper;
using MediatR;
using XmlEmailSender.Application.Common.Errors;
using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Application.Documents.Dtos;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Documents.Queries.GetDocumentById;

public sealed record GetDocumentByIdQuery(Guid Id) : IQuery<DocumentDto>;

internal sealed class GetDocumentByIdQueryHandler
    : IRequestHandler<GetDocumentByIdQuery, Result<DocumentDto>>
{
    private readonly IElectronicDocumentRepository _repo;
    private readonly IMapper _mapper;

    public GetDocumentByIdQueryHandler(IElectronicDocumentRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<Result<DocumentDto>> Handle(GetDocumentByIdQuery request, CancellationToken ct)
    {
        var doc = await _repo.GetByIdAsync(request.Id, ct);
        return doc is null
            ? Result.Failure<DocumentDto>(ApplicationErrors.Document.NotFound)
            : Result.Success(_mapper.Map<DocumentDto>(doc));
    }
}
