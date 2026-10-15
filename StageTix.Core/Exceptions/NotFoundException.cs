namespace StageTix.Core.Exceptions;

/// <summary>The requested resource does not exist, or the caller is not allowed to know it exists. Maps to 404.</summary>
public class NotFoundException(string resourceName, object key)
    : AppException($"{resourceName} with id '{key}' was not found.");
