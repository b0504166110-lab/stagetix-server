using StageTix.API.Errors;
using StageTix.Core.Exceptions;

namespace StageTix.API.Middleware;

/// <summary>
/// Global error handler. Catches every exception thrown further down the pipeline and turns it
/// into the uniform JSON error response, so controllers contain no try/catch.
/// Must be the first middleware in the pipeline so it wraps everything after it.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            OperationCanceledException when context.RequestAborted.IsCancellationRequested =>
                (StatusCodes.Status499ClientClosedRequest, "Client Closed Request", "The request was cancelled."),
            // Never send internal details (stack trace, SQL) to the client.
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An unexpected error occurred. Please try again later.")
        };

        // Log levels: an expected, handled situation is Information or Warning; a real failure is Error.
        switch (exception)
        {
            case ConflictException:
                logger.LogWarning("Request rejected with 409 Conflict: {Detail}", exception.Message);
                break;
            case AppException:
                logger.LogInformation("Request rejected with {Status}: {Detail}", status, exception.Message);
                break;
            case OperationCanceledException when status == StatusCodes.Status499ClientClosedRequest:
                logger.LogInformation("Request cancelled by the client");
                break;
            default:
                logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                break;
        }

        await ErrorResponses.WriteAsync(context, status, title, detail);
    }
}
