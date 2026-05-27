using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public abstract class DomainException : Exception
{
    public Error Error { get; }

    protected DomainException(Error error) : base(error.Message)
    {
        Error = error;
    }

    protected DomainException(string message) : base(message)
    {
        Error = Error.None;
    }
}
