using OrderHub.Domain.Common;
using OrderHub.Domain.Users;

namespace OrderHub.Domain.Orders;

public class Order : BaseEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public Guid UserId { get; set; }
    public OrderStatusEnum Status { get; set; } = OrderStatusEnum.Pending;
    public decimal TotalAmount { get; set; }

    // Shipping information
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = [];
    public string Note { get; set; } = string.Empty;
}
