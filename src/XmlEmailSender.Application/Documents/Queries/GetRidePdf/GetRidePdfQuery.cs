using MediatR;
using XmlEmailSender.Application.Abstractions.Pdf;
using XmlEmailSender.Application.Common.Errors;
using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Documents.Queries.GetRidePdf;

public sealed record GetRidePdfQuery(Guid DocumentId) : IQuery<RidePdfDto>;

public sealed record RidePdfDto(string FileName, byte[] Content);

internal sealed class GetRidePdfQueryHandler
    : IRequestHandler<GetRidePdfQuery, Result<RidePdfDto>>
{
    private readonly IElectronicDocumentRepository _repo;
    private readonly IRideGenerator _ride;

    public GetRidePdfQueryHandler(IElectronicDocumentRepository repo, IRideGenerator ride)
    {
        _repo = repo;
        _ride = ride;
    }

    public async Task<Result<RidePdfDto>> Handle(GetRidePdfQuery request, CancellationToken ct)
    {
        var doc = await _repo.GetByIdAsync(request.DocumentId, ct);
        if (doc is null)
            return Result.Failure<RidePdfDto>(ApplicationErrors.Document.NotFound);

        var pdf = _ride.Generate(doc);
        var safe = doc.DocumentNumber.Replace("/", "-").Replace(" ", "_");
        return Result.Success(new RidePdfDto($"{safe}.pdf", pdf));
    }
}
