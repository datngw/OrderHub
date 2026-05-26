using MediatR;
using OrderHub.Application.Common.Results;

namespace OrderHub.Application.Common.Messaging;

public interface IQuery<TResult> : IRequest<Result<TResult>>;
