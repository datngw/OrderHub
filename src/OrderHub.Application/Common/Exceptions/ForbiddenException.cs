using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException(Error error) : base(error) { }
    public ForbiddenException(string message) : base(message) { }
}
