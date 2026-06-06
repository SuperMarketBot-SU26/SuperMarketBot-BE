using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// OTP gửi email — dùng cho 2 luồng:
///   "Registration"  → xác thực email khi đăng ký
///   "PasswordReset" → đặt lại mật khẩu
/// </summary>
public class EmailOtp
{
    public Guid OtpId { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    /// <summary>6 ký tự số</summary>
    public string OtpCode { get; set; } = string.Empty;

    /// <summary>"Registration" | "PasswordReset"</summary>
    public string OtpType { get; set; } = "Registration";

    public DateTime ExpiredAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = VnDateTime.Now;

    // Tạm lưu dữ liệu đăng ký cho đến khi OTP được xác nhận
    public string? TemporaryPasswordHash { get; set; }
    public string? TemporaryFullName { get; set; }
    public string? TemporaryPhone { get; set; }
}
