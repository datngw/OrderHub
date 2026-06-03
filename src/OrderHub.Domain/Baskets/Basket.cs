namespace OrderHub.Domain.Baskets;

public class Basket
{
    public Guid UserId { get; set; }
    public List<BasketItem> Items { get; set; } = [];
    public decimal TotalAmount => Items.Sum(i => i.UnitPrice * i.Quantity);
    public int TotalItems => Items.Sum(i => i.Quantity);
}
