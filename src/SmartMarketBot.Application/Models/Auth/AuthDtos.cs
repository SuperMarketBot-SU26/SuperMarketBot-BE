namespace SmartMarketBot.Application.Models.Auth;

// ── Request DTOs ────────────────────────────────────────────────────

/// <summary>Bước 1: gửi OTP đăng ký về email</summary>
public sealed record RegisterRequestOtpDto(
    string FullName,
    string Email,
    string? Phone,
    string Password);

/// <summary>Bước 2: xác thực OTP → tạo tài khoản + trả về token</summary>
public sealed record VerifyOtpDto(string Email, string OtpCode);

public sealed record ResendOtpDto(string Email);

public sealed record LoginRequestDto(string Email, string Password);

public sealed record RefreshTokenRequestDto(string RefreshToken);

public sealed record ForgotPasswordDto(string Email);

public sealed record ResetPasswordDto(string Email, string OtpCode, string NewPassword);

// ── Response DTOs ───────────────────────────────────────────────────

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    int UserId,
    string Email,
    string? FullName,
    IReadOnlyList<string> Roles);

// Legacy — giữ tương thích AuthService cũ nếu có code dùng
public sealed record RegisterRequestDto(string Username, string Password, string? Email);

// ── Face Login DTOs ──────────────────────────────────────────────────
public sealed record FaceLoginRequestDto(string ImageBase64);

public sealed record FaceLoginResponseDto(
    bool Success,
    string? Message,
    string? Greeting,
    AuthResponseDto? Token,
    FaceLoginMemberDto? Member);

public sealed record FaceLoginMemberDto(
    int MemberId,
    string FullName,
    string PhoneNumber,
    string Tier,
    int TotalPoints);
