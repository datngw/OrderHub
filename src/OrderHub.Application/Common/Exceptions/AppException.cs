using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Exceptions;

public class AppException(Error error) : Exception(error.Description)
{
    public Error Error { get; } = error;
}
