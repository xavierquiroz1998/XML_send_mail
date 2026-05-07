using AutoMapper;
using MediatR;
using XmlEmailSender.Application.Abstractions.Security;
using XmlEmailSender.Application.Common.Errors;
using XmlEmailSender.Application.Smtp.Dtos;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Emails;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Smtp.Commands.SaveSmtpConfiguration;

internal sealed class SaveSmtpConfigurationCommandHandler
    : IRequestHandler<SaveSmtpConfigurationCommand, Result<SmtpConfigurationDto>>
{
    private readonly ISmtpConfigurationRepository _repo;
    private readonly IPasswordProtector _protector;
    private readonly IMapper _mapper;

    public SaveSmtpConfigurationCommandHandler(
        ISmtpConfigurationRepository repo,
        IPasswordProtector protector,
        IMapper mapper)
    {
        _repo = repo;
        _protector = protector;
        _mapper = mapper;
    }

    public async Task<Result<SmtpConfigurationDto>> Handle(
        SaveSmtpConfigurationCommand request,
        CancellationToken ct)
    {
        if (request.Id is null)
        {
            // CREATE — password obligatoria, ya validada.
            var encrypted = _protector.Protect(request.NewPassword!);
            var entity = new SmtpConfiguration(
                request.Name, request.Host, request.Port, request.UseSsl,
                request.Username, encrypted, request.FromEmail, request.FromName);

            if (request.Activate)
                await DeactivateOthersAsync(except: entity.Id, ct);
            else
                entity.Deactivate();

            await _repo.AddAsync(entity, ct);
            return Result.Success(_mapper.Map<SmtpConfigurationDto>(entity));
        }

        // UPDATE
        var existing = await _repo.GetByIdAsync(request.Id.Value, ct);
        if (existing is null)
            return Result.Failure<SmtpConfigurationDto>(ApplicationErrors.Smtp.NotFound);

        string? newEncrypted = string.IsNullOrEmpty(request.NewPassword)
            ? null
            : _protector.Protect(request.NewPassword);

        existing.Update(request.Name, request.Host, request.Port, request.UseSsl,
            request.Username, newEncrypted, request.FromEmail, request.FromName);

        if (request.Activate)
        {
            existing.Activate();
            await DeactivateOthersAsync(except: existing.Id, ct);
        }
        else
        {
            existing.Deactivate();
        }

        await _repo.UpdateAsync(existing, ct);
        return Result.Success(_mapper.Map<SmtpConfigurationDto>(existing));
    }

    private async Task DeactivateOthersAsync(Guid except, CancellationToken ct)
    {
        var all = await _repo.ListAsync(ct);
        foreach (var other in all.Where(c => c.Id != except && c.IsActive))
        {
            other.Deactivate();
            await _repo.UpdateAsync(other, ct);
        }
    }
}
