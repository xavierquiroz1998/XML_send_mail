using Microsoft.AspNetCore.Mvc;

namespace XmlEmailSender.API.Middleware;

/// <summary>
/// Captura excepciones no controladas y las traduce a ProblemDetails (RFC 7807).
/// No "esconde" los Result.Failure: esos van por ResultExtensions.ToActionResult
/// directamente. Aquí solo tratamos lo verdaderamente inesperado (NRE, fallos
/// de IO, etc.) — todo lo logueamos para correlación.
/// </summary>
internal sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                // Demasiado tarde para reescribir la respuesta — solo logueamos.
                throw;
            }

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = _env.IsDevelopment() ? ex.ToString() : "Ocurrió un error inesperado.",
                Type = "https://xmlemailsender/errors/InternalServerError",
                Instance = context.Request.Path
            };

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
