using System;

namespace LIGHTNING.Core.Exceptions;

public sealed class BoundaryViolationException : Exception
{
    public BoundaryViolationException(string message) : base(message) { }
}
