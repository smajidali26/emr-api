namespace EMR.Domain.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleViolationException(string ruleName, string message)
        : base($"Business rule '{ruleName}' violation: {message}")
    {
        RuleName = ruleName;
    }

    public BusinessRuleViolationException(string ruleName, string message, Exception innerException)
        : base($"Business rule '{ruleName}' violation: {message}", innerException)
    {
        RuleName = ruleName;
    }
}
