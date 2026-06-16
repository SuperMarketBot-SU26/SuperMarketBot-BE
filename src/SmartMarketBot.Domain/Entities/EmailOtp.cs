namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Bảng lưu mã OTP dùng cho email verification (registration, password reset, etc.).
/// KHÔNG thuộc ERD V4.0 cốt lõi - giữ riêng vì là bảng Identity phụ trợ cho AuthService.
/// </summary>
public class EmailOtp
{
    public Guid OtpId { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string OtpType { get; set; } = "Registration";
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiredAt { get; set; } = DateTime.UtcNow.AddMinutes(10);
    public string? TemporaryFullName { get; set; }
    public string? TemporaryPhone { get; set; }

    /// <summary>Hash mật khẩu tạm thời (giữ cho tương thích AuthService cũ).</summary>
    public string? TemporaryPasswordHash { get; set; }
}
