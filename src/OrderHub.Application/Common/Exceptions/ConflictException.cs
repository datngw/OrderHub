using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(Error error) : base(error) { }
}
