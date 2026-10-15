using System.Text.Json;
using StageTix.Core.DTOs.Common;

namespace StageTix.API.Errors;

/// <summary>
/// Builds and writes the single JSON error shape used by every part of the API:
/// the exception middleware, model validation, and the 401/403 responses of JWT authentication.
/// </summary>
public static class ErrorResponses
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ErrorResponse Create(
        HttpContext context, int status, string title, string detail, IDictionary<string, string[]>? errors = null) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        CorrelationId = context.TraceIdentifier,
        Errors = errors
    };

    public static Task WriteAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(Create(context, status, title, detail), JsonOptions);
    }
}
