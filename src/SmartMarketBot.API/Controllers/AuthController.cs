using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Auth;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    // ── REGISTER ────────────────────────────────────────────────────

    /// <summary>Bước 1: Gửi OTP đăng ký về email</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterRequest(
        [FromBody] RegisterRequestOtpDto request, CancellationToken ct)
    {
        await authService.RegisterRequestOtpAsync(request, ct);
        return Ok(new { message = "Mã OTP đã được gửi về email của bạn." });
    }

    /// <summary>Bước 2: Xác thực OTP → tạo tài khoản + nhận JWT</summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp(
        [FromBody] VerifyOtpDto request, CancellationToken ct)
    {
        var result = await authService.VerifyOtpAndRegisterAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Gửi lại OTP (cooldown 60s)</summary>
    [HttpPost("resend-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendOtpDto request, CancellationToken ct)
    {
        await authService.ResendOtpAsync(request, ct);
        return Ok(new { message = "OTP mới đã được gửi." });
    }

    // ── LOGIN / TOKEN ────────────────────────────────────────────────

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Đăng nhập bằng nhận diện khuôn mặt</summary>
    [HttpPost("face-login")]
    [HttpPost("login-face")]
    [ProducesResponseType(typeof(FaceLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FaceLoginResponseDto>> FaceLogin(
        [FromBody] FaceLoginRequestDto request, CancellationToken ct)
    {
        var result = await authService.FaceLoginAsync(request, ct);
        if (!result.Success)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    /// <summary>Làm mới Access Token bằng Refresh Token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        [FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Đăng ký khuôn mặt cho tài khoản đang đăng nhập</summary>
    [Authorize]
    [HttpPost("register-face")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterFace(
        [FromBody] FaceLoginRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var success = await authService.RegisterFaceAsync(userId.Value, request, ct);
        if (!success)
        {
            return BadRequest(new { message = "Đăng ký khuôn mặt thất bại. Vui lòng chụp ảnh rõ nét hơn." });
        }

        return Ok(new { message = "Đăng ký khuôn mặt thành công." });
    }

    /// <summary>Đăng xuất — revoke refresh token hiện tại</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();
        await authService.LogoutAsync(userId.Value, request.RefreshToken, ct);
        return Ok(new { message = "Đăng xuất thành công." });
    }

    // ── PASSWORD RESET ───────────────────────────────────────────────

    /// <summary>Bước 1 quên mật khẩu: gửi OTP reset về email</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto request, CancellationToken ct)
    {
        await authService.ForgotPasswordAsync(request, ct);
        return Ok(new { message = "Nếu email tồn tại, mã OTP đã được gửi." });
    }

    /// <summary>Bước 2 quên mật khẩu: xác thực OTP + đặt mật khẩu mới</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto request, CancellationToken ct)
    {
        await authService.ResetPasswordAsync(request, ct);
        return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
    }

    // ── HELPERS ─────────────────────────────────────────────────────

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }
}
