using MediatR;
using XmlEmailSender.Application.Common.Errors;
using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Smtp.Commands.DeleteSmtpConfiguration;

public sealed record DeleteSmtpConfigurationCommand(Guid Id) : ICommand;

internal sealed class DeleteSmtpConfigurationCommandHandler
    : IRequestHandler<DeleteSmtpConfigurationCommand, Result>
{
    private readonly ISmtpConfigurationRepository _repo;
    public DeleteSmtpConfigurationCommandHandler(ISmtpConfigurationRepository repo) => _repo = repo;

    public async Task<Result> Handle(DeleteSmtpConfigurationCommand request, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(request.Id, ct);
        if (existing is null) return Result.Failure(ApplicationErrors.Smtp.NotFound);

        await _repo.RemoveAsync(request.Id, ct);
        return Result.Success();
    }
}
