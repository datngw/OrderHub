using OrderHub.Domain.Common;
using OrderHub.Domain.Users;

namespace OrderHub.Domain.Orders;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public OrderStatusEnum Status { get; set; } = OrderStatusEnum.Pending;
    public decimal TotalAmount { get; set; }

    public User User { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = [];
}
