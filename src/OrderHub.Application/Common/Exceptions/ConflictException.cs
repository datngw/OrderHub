using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(Error error) : base(error) { }
    public ConflictException(string message) : base(message) { }
}
