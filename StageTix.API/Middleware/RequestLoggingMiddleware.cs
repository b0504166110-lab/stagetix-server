using System.Diagnostics;

namespace StageTix.API.Middleware;

/// <summary>
/// Writes one log line for every incoming request and one when it completes (status and duration).
/// Only the method and path are logged — never headers (they hold the token) or bodies (they hold passwords).
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value;

        logger.LogInformation("HTTP {Method} {Path} started", method, path);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            // The exception middleware (earlier in the pipeline) logs it and writes the error response.
            logger.LogDebug("HTTP {Method} {Path} threw after {ElapsedMs} ms", method, path, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
