using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(Error error) : base(error) { }
    public NotFoundException(string message) : base(message) { }
}
