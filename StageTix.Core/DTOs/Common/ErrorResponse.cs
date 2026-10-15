namespace StageTix.Core.DTOs.Common;

/// <summary>The single JSON shape returned for every error.</summary>
public class ErrorResponse
{
    /// <summary>HTTP status code.</summary>
    public int Status { get; set; }

    /// <summary>Short title, e.g. "Conflict".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>A message that can be shown to the user.</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>Identifies the request in the server logs.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Field validation errors (400 only): field name → messages.</summary>
    public IDictionary<string, string[]>? Errors { get; set; }
}
