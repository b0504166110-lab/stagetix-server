namespace StageTix.Core.Exceptions;

/// <summary>
/// Someone else changed the same data between our read and our save (optimistic concurrency).
/// Thrown by the Data layer when EF Core reports a concurrency conflict, so the Service layer
/// does not need to know EF Core. Maps to 409 and is logged as a Warning.
/// </summary>
public class ConcurrencyConflictException(string message, Exception? innerException = null)
    : ConflictException(message, innerException);
