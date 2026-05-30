using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(Error error) : base(error) { }
}
