namespace SmartMarketBot.Application.Interfaces;

public interface IEmailService
{
    /// <summary>Gửi email OTP đăng ký</summary>
    Task SendRegistrationOtpAsync(string toEmail, string fullName, string otpCode, CancellationToken ct = default);

    /// <summary>Gửi email OTP quên mật khẩu</summary>
    Task SendPasswordResetOtpAsync(string toEmail, string otpCode, CancellationToken ct = default);

    /// <summary>Generic — dùng cho thông báo khác nếu cần</summary>
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
