using MediatR;
using Microsoft.Extensions.Logging;
using XmlEmailSender.Application.Common.Messaging;
using XmlEmailSender.Domain.Common;
using XmlEmailSender.Domain.Repositories;

namespace XmlEmailSender.Application.Common.Behaviors;

/// <summary>
/// Abre transacción al inicio del Command, hace commit si el handler
/// retorna éxito o rollback si retorna Failure / lanza excepción.
/// Aplica solo a ICommand / ICommand&lt;T&gt;: las queries no necesitan tx.
/// </summary>
internal sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UnitOfWorkBehavior<TRequest, TResponse>> _logger;

    public UnitOfWorkBehavior(IUnitOfWork uow, ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand(request)) return await next();

        await _uow.BeginAsync(cancellationToken);
        try
        {
            var response = await next();

            if (response is Result r && r.IsFailure)
            {
                _logger.LogDebug("Rolling back: handler returned Failure {Code}", r.Error.Code);
                await _uow.RollbackAsync(cancellationToken);
                return response;
            }

            await _uow.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsCommand(TRequest request)
        => request is ICommand
        || request.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
}
