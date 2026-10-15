namespace StageTix.Core.Exceptions;

/// <summary>Wrong email or password. Maps to 401. The message never says which of the two was wrong.</summary>
public class AuthenticationFailedException() : AppException("Invalid email or password.");
