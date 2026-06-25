namespace SmartMarketBot.Domain.Entities;

public class Account
{
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? FullName { get; set; }

    /// <summary>URL ảnh đại diện hiển thị trên UI (Cloudinary hoặc bất kỳ CDN nào).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Enum dạng string: 'Active' | 'Inactive' | 'Pending' | 'Blocked'.</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Enum dạng string: 'Admin' | 'Staff' | 'Member'.</summary>
    public string Role { get; set; } = "Member";

    // OTP fields - gộp từ bảng EMAIL_OTP cũ
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiredAt { get; set; }
    public string? OtpType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Refresh token (gộp từ bảng USER_TOKEN cũ - chỉ giữ 1 token/Account)
    public string? RefreshToken { get; set; }
    public DateTime? RefreshExpiry { get; set; }
    public bool IsTokenRevoked { get; set; } = false;

    // Navigation
    public virtual Member? Member { get; set; }
}
