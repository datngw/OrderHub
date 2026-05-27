using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Common.Messaging;

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, Result<TResult>>
    where TQuery : IQuery<TResult>;
