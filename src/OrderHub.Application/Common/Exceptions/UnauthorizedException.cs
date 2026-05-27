using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(Error error) : base(error) { }
    public UnauthorizedException(string message) : base(message) { }
}
