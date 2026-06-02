using SmartMarketBot.Application.Models.Auth;

namespace SmartMarketBot.Application.Interfaces;

public interface IAuthService
{
    /// <summary>Bước 1 đăng ký: hash password, lưu OTP tạm → gửi email</summary>
    Task RegisterRequestOtpAsync(RegisterRequestOtpDto request, CancellationToken ct = default);

    /// <summary>Bước 2 đăng ký: xác thực OTP → tạo User + trả về JWT</summary>
    Task<AuthResponseDto> VerifyOtpAndRegisterAsync(VerifyOtpDto request, CancellationToken ct = default);

    /// <summary>Gửi lại OTP (còn hạn 60s mới được resend)</summary>
    Task ResendOtpAsync(ResendOtpDto request, CancellationToken ct = default);

    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);

    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default);

    Task LogoutAsync(int userId, string refreshToken, CancellationToken ct = default);

    /// <summary>Bước 1 quên mật khẩu: gửi OTP</summary>
    Task ForgotPasswordAsync(ForgotPasswordDto request, CancellationToken ct = default);

    /// <summary>Bước 2 quên mật khẩu: xác thực OTP + đặt lại mật khẩu</summary>
    Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken ct = default);
}
