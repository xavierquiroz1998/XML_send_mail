using AutoMapper;
using MediatR;
using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Application.Smtp.Dtos;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Smtp.Queries.ListSmtpConfigurations;

public sealed record ListSmtpConfigurationsQuery() : IQuery<IReadOnlyList<SmtpConfigurationDto>>;

internal sealed class ListSmtpConfigurationsQueryHandler
    : IRequestHandler<ListSmtpConfigurationsQuery, Result<IReadOnlyList<SmtpConfigurationDto>>>
{
    private readonly ISmtpConfigurationRepository _repo;
    private readonly IMapper _mapper;

    public ListSmtpConfigurationsQueryHandler(ISmtpConfigurationRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<SmtpConfigurationDto>>> Handle(
        ListSmtpConfigurationsQuery request, CancellationToken ct)
    {
        var items = await _repo.ListAsync(ct);
        return Result.Success<IReadOnlyList<SmtpConfigurationDto>>(
            _mapper.Map<List<SmtpConfigurationDto>>(items));
    }
}
