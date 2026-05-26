namespace OrderHub.Domain.Orders;

public enum OrderStatusEnum
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
