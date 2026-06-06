namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Lưu refresh token — mỗi device/session 1 row.
/// Revoke khi logout, hoặc cấp mới thì revoke cái cũ (rotation).
/// </summary>
public class UserToken
{
    public Guid TokenId { get; set; } = Guid.NewGuid();
    public int AccountId { get; set; }

    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Device/browser info (tuỳ chọn — hữu ích cho "active sessions")</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>IP lúc cấp token</summary>
    public string? IpAddress { get; set; }

    public virtual Account Account { get; set; } = null!;
}
