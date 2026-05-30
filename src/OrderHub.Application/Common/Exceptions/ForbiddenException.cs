using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(Error error) : base(error) { }
}
