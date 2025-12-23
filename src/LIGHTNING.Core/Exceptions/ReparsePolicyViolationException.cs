namespace LIGHTNING.Core.Exceptions;

/// <summary>
/// Exception thrown when a reparse-point violation occurs. Distinguishes
/// reparse policy failures from generic boundary violations.
/// </summary>
public sealed class ReparsePolicyViolationException : BoundaryViolationException
{
    public ReparsePolicyViolationException() { }

    public ReparsePolicyViolationException(string message)
        : base(message) { }

    public ReparsePolicyViolationException(string message, System.Exception innerException)
        : base(message, innerException) { }
}
