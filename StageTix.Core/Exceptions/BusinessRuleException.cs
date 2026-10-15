namespace StageTix.Core.Exceptions;

/// <summary>
/// The request is well-formed but breaks a rule that depends on the data,
/// e.g. a seat that does not belong to the show. Maps to 400.
/// </summary>
public class BusinessRuleException(string message) : AppException(message);
