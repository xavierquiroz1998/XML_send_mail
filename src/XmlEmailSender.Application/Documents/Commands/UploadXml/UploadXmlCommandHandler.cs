using AutoMapper;
using MediatR;
using XmlEmailSender.Application.Abstractions.Parsing;
using XmlEmailSender.Application.Documents.Dtos;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Documents.Commands.UploadXml;

internal sealed class UploadXmlCommandHandler
    : IRequestHandler<UploadXmlCommand, Result<DocumentDto>>
{
    private readonly IXmlDocumentParser _parser;
    private readonly IElectronicDocumentRepository _repo;
    private readonly IMapper _mapper;

    public UploadXmlCommandHandler(
        IXmlDocumentParser parser,
        IElectronicDocumentRepository repo,
        IMapper mapper)
    {
        _parser = parser;
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<Result<DocumentDto>> Handle(UploadXmlCommand request, CancellationToken ct)
    {
        var parseResult = _parser.Parse(request.XmlContent);
        if (parseResult.IsFailure)
            return Result.Failure<DocumentDto>(parseResult.Error);

        var doc = parseResult.Value;

        // Idempotencia: si ya existe la clave de acceso, devolvemos el existente.
        var existing = await _repo.GetByAccessKeyAsync(doc.AccessKey.Value, ct);
        if (existing != null)
            return Result.Success(_mapper.Map<DocumentDto>(existing));

        await _repo.AddAsync(doc, ct);
        return Result.Success(_mapper.Map<DocumentDto>(doc));
    }
}
