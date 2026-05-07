using FluentValidation;
using MediatR;
using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Application.Common.Behaviors;

/// <summary>
/// Ejecuta todos los IValidator&lt;TRequest&gt; registrados antes del handler.
/// Si hay errores y TResponse es Result o Result&lt;T&gt;, devuelve un Failure
/// agregando el primer error de validación, sin lanzar excepciones.
/// Si TResponse no es un Result, lanza ValidationException (rara vez ocurrirá
/// porque todos nuestros handlers retornan Result).
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var ctx = new ValidationContext<TRequest>(request);
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var v in _validators)
        {
            var r = await v.ValidateAsync(ctx, cancellationToken);
            if (!r.IsValid) failures.AddRange(r.Errors);
        }
        if (failures.Count == 0) return await next();

        var first = failures[0];
        var error = Error.Validation($"Validation.{first.PropertyName}", first.ErrorMessage);

        // Si el handler retorna Result o Result<T>, materializamos un Failure tipado.
        var responseType = typeof(TResponse);
        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var inner = responseType.GenericTypeArguments[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .Single(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                .MakeGenericMethod(inner);
            return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
        }

        // Caso fallback: no es Result. Lanzamos para no enmascarar.
        throw new ValidationException(failures);
    }
}
