namespace LIGHTNING.Core.Exceptions;

public class BoundaryViolationException : System.Exception
{
    public BoundaryViolationException() { }

    public BoundaryViolationException(string message)
        : base(message) { }

    public BoundaryViolationException(string message, System.Exception innerException)
        : base(message, innerException) { }
}
