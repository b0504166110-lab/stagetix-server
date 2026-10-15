using System.Text.RegularExpressions;

namespace StageTix.API.Middleware;

/// <summary>
/// Gives every request a correlation id: reuses a safe X-Correlation-ID sent by the caller, or creates one.
/// The id is stored in HttpContext.TraceIdentifier (NLog writes it on every log line through
/// ${aspnet-TraceIdentifier}), returned in the X-Correlation-ID response header and in every error body,
/// so a user's error message can be matched to the exact server log lines.
/// </summary>
public partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();

        // Only short, plain ids are accepted, so a caller cannot inject text into the logs.
        var correlationId = SafeId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        return next(context);
    }

    [GeneratedRegex("^[A-Za-z0-9-]{8,64}$")]
    private static partial Regex SafeId();
}
