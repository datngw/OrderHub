using OrderHub.Domain.Common;
using OrderHub.Domain.Orders;

namespace OrderHub.Domain.Users;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRoleEnum Role { get; set; } = UserRoleEnum.Customer;

    public List<Order> Orders { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
