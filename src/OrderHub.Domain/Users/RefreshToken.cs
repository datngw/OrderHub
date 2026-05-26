using OrderHub.Domain.Common;

namespace OrderHub.Domain.Users;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired(DateTimeOffset? now = null) => ExpiresAt < (now ?? DateTimeOffset.UtcNow).DateTime;
    public bool IsActive(DateTimeOffset? now = null) => !IsRevoked && !IsExpired(now);

    public static RefreshToken Create(string token, Guid userId, DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = expiresAt.DateTime
        };
    }

    public void Revoke(Guid? replacedByTokenId = null)
    {
        IsRevoked = true;
        ReplacedByTokenId = replacedByTokenId;
    }
}
