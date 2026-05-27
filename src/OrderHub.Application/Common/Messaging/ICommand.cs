using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Messaging;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResult> : IRequest<Result<TResult>>;
