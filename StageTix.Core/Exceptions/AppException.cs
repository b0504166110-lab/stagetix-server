namespace StageTix.Core.Exceptions;

/// <summary>
/// Base class for expected, handled errors. The global error middleware maps each subclass
/// to an HTTP status code; the message is safe to show to the user.
/// </summary>
public abstract class AppException(string message, Exception? innerException = null)
    : Exception(message, innerException);
