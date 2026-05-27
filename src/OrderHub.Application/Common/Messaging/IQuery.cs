using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Messaging;

public interface IQuery<TResult> : IRequest<Result<TResult>>;
