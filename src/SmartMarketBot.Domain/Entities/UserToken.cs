namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Refresh token lưu trong DB để cấp lại access token khi hết hạn.
/// Bảng phụ trợ Identity - không thuộc ERD V4.0 cốt lõi.
/// </summary>
public class UserToken
{
    public Guid TokenId { get; set; } = Guid.NewGuid();
    public int AccountId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Account? Account { get; set; }
}
