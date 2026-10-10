namespace Identity.Domain.Exceptions;

/// <summary>
/// Raised when a request cannot be completed because it conflicts with the current state of
/// a resource — registering an email that already exists, for example. Maps to HTTP 409.
/// </summary>
/// <remarks>
/// A dedicated type exists so a domain conflict stays distinguishable from a framework
/// exception. Conflicts used to be signalled with <see cref="InvalidOperationException"/>,
/// which the API mapped to 409 wholesale; because Npgsql reports an unreachable database as
/// a plain <see cref="InvalidOperationException"/> too, a database outage answered
/// "409 Conflict" instead of a 5xx.
/// </remarks>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
