using OrderHub.Domain.Common;

namespace OrderHub.Domain.Users;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    public User User { get; set; } = null!;
}
