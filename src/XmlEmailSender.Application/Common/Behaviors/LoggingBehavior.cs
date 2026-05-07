using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Application.Common.Behaviors;

/// <summary>
/// Loguea cada request con duración + resultado (éxito/error).
/// Reflection mínima: si TResponse es Result o Result&lt;T&gt;, leemos
/// IsSuccess para enriquecer el log; en otros casos asumimos éxito.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("[CQRS] -> {Request}", name);

        try
        {
            var response = await next();
            sw.Stop();

            if (response is Result r)
            {
                if (r.IsSuccess)
                    _logger.LogInformation("[CQRS] <- {Request} OK ({Elapsed} ms)", name, sw.ElapsedMilliseconds);
                else
                    _logger.LogWarning("[CQRS] <- {Request} FAIL {Code}: {Message} ({Elapsed} ms)",
                        name, r.Error.Code, r.Error.Message, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("[CQRS] <- {Request} OK ({Elapsed} ms)", name, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[CQRS] <!> {Request} threw after {Elapsed} ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
