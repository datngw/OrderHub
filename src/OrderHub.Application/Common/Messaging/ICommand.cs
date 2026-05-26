using MediatR;
using OrderHub.Application.Common.Results;

namespace OrderHub.Application.Common.Messaging;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResult> : IRequest<Result<TResult>>;
