namespace StageTix.Core.Exceptions;

/// <summary>
/// The request conflicts with the current state of a resource,
/// e.g. the email is already registered or the seat is already sold. Maps to 409.
/// </summary>
public class ConflictException(string message, Exception? innerException = null)
    : AppException(message, innerException);
