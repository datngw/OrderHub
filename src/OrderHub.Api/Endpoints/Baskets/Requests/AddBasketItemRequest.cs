namespace OrderHub.Api.Endpoints.Baskets.Requests;

public record AddBasketItemRequest(Guid ProductId, int Quantity);
