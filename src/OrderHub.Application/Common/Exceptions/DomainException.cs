using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

/// <summary>
/// Base exception for domain-level errors. Carries a typed <see cref="Error"/> for consistent mapping to HTTP responses.
/// </summary>
public abstract class DomainException : Exception
{
    public Error Error { get; }

    protected DomainException(Error error) : base(error.Message)
    {
        Error = error;
    }
}
