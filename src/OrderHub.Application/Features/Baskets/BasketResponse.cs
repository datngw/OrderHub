namespace OrderHub.Application.Features.Baskets;

public record BasketResponse(
    Guid UserId,
    decimal TotalAmount,
    int TotalItems,
    List<BasketItemResponse> Items);

public record BasketItemResponse(
    Guid ProductId,
    string ProductName,
    string SKU,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
