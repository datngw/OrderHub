using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(Error error) : base(error) { }
}
