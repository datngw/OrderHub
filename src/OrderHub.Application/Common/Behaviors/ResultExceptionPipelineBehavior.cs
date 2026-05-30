using MediatR;
using OrderHub.Application.Common.Exceptions;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Behaviors;

public sealed class ResultExceptionPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (response is Result { IsFailure: true } result)
        {
            throw new AppException(result.Error);
        }

        return response;
    }
}
