using Microsoft.AspNetCore.Mvc;
using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.API.Common;

/// <summary>
/// Mapea Result / Result&lt;T&gt; del dominio a IActionResult.
/// El Type del Error decide el HTTP status:
///   Validation -> 400
///   NotFound   -> 404
///   Conflict   -> 409
///   Failure    -> 500 (fallo interno controlado)
/// El payload de error sigue ProblemDetails (RFC 7807).
/// </summary>
internal static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
        => result.IsSuccess
            ? new NoContentResult()
            : Problem(result.Error);

    public static IActionResult ToActionResult<T>(this Result<T> result)
        => result.IsSuccess
            ? new OkObjectResult(result.Value)
            : Problem(result.Error);

    public static IActionResult ToCreatedResult<T>(
        this Result<T> result,
        Func<T, string> locationFactory)
        => result.IsSuccess
            ? new CreatedResult(locationFactory(result.Value), result.Value)
            : Problem(result.Error);

    private static IActionResult Problem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Message,
            Type = $"https://xmlemailsender/errors/{error.Code}"
        };

        return new ObjectResult(problem) { StatusCode = status };
    }
}
