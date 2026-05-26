using OrderHub.Application.Common.Messaging;

namespace OrderHub.Application.Features.Products.DeleteProduct;

public record DeleteProductCommand(Guid Id) : ICommand;
