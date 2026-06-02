using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Auth;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AuthService(
    AppDbContext db,
    ITokenService tokenService,
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly EmailOptions _emailOpts = emailOptions.Value;
    private readonly JwtOptions _jwtOpts = jwtOptions.Value;

    // ────────────────────── REGISTER FLOW ──────────────────────────

    public async Task RegisterRequestOtpAsync(RegisterRequestOtpDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await db.Users.AnyAsync(u => u.Email == email, ct);
        if (existingUser)
            throw new InvalidOperationException("Email đã được sử dụng.");

        var recentOtp = await db.EmailOtps
            .Where(o => o.Email == email && o.OtpType == "Registration" && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recentOtp != null)
        {
            var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
            if (DateTime.UtcNow - recentOtp.CreatedAt < cooldown)
                throw new InvalidOperationException($"Vui lòng chờ {_emailOpts.OtpResendCooldownSeconds}s trước khi yêu cầu OTP mới.");
        }

        var otpCode = GenerateOtp();
        var otp = new EmailOtp
        {
            Email = email,
            OtpCode = otpCode,
            OtpType = "Registration",
            ExpiredAt = DateTime.UtcNow.AddMinutes(_emailOpts.OtpExpiryMinutes),
            TemporaryPasswordHash = HashPassword(request.Password),
            TemporaryFullName = request.FullName,
            TemporaryPhone = request.Phone
        };

        db.EmailOtps.Add(otp);
        await db.SaveChangesAsync(ct);

        await emailService.SendRegistrationOtpAsync(email, request.FullName, otpCode, ct);
        logger.LogInformation("[Auth] Registration OTP sent → {Email}", email);
    }

    public async Task<AuthResponseDto> VerifyOtpAndRegisterAsync(VerifyOtpDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var otp = await db.EmailOtps
            .Where(o => o.Email == email && o.OtpType == "Registration" && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        ValidateOtp(otp, request.OtpCode);

        // Tạo user
        var user = new User
        {
            Username = email, // dùng email làm username mặc định
            Email = email,
            PasswordHash = otp!.TemporaryPasswordHash!,
            FullName = otp.TemporaryFullName,
            Phone = otp.TemporaryPhone,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        otp.IsUsed = true;
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        // Gán role Member mặc định
        var memberRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Member", ct);
        if (memberRole != null)
        {
            db.UserRoles.Add(new UserRole { UserID = user.UserID, RoleID = memberRole.RoleID });
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("[Auth] User registered: {Email}", email);
        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task ResendOtpAsync(ResendOtpDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var lastOtp = await db.EmailOtps
            .Where(o => o.Email == email && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (lastOtp == null)
            throw new InvalidOperationException("Không tìm thấy yêu cầu OTP. Vui lòng bắt đầu lại.");

        var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
        if (DateTime.UtcNow - lastOtp.CreatedAt < cooldown)
            throw new InvalidOperationException($"Vui lòng chờ {_emailOpts.OtpResendCooldownSeconds}s trước khi gửi lại.");

        // Vô hiệu hoá OTP cũ → tạo mới
        lastOtp.IsUsed = true;
        var newCode = GenerateOtp();
        var newOtp = new EmailOtp
        {
            Email = email,
            OtpCode = newCode,
            OtpType = lastOtp.OtpType,
            ExpiredAt = DateTime.UtcNow.AddMinutes(_emailOpts.OtpExpiryMinutes),
            TemporaryPasswordHash = lastOtp.TemporaryPasswordHash,
            TemporaryFullName = lastOtp.TemporaryFullName,
            TemporaryPhone = lastOtp.TemporaryPhone
        };
        db.EmailOtps.Add(newOtp);
        await db.SaveChangesAsync(ct);

        if (lastOtp.OtpType == "PasswordReset")
            await emailService.SendPasswordResetOtpAsync(email, newCode, ct);
        else
            await emailService.SendRegistrationOtpAsync(email, lastOtp.TemporaryFullName ?? email, newCode, ct);
    }

    // ────────────────────── LOGIN / TOKEN ──────────────────────────

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

        if (!user.EmailConfirmed)
            throw new UnauthorizedAccessException("Email chưa được xác minh. Vui lòng kiểm tra hộp thư.");

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default)
    {
        var tokenEntity = await db.UserTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.RefreshToken == request.RefreshToken && !t.IsRevoked, ct);

        if (tokenEntity is null || tokenEntity.ExpiryDate < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn.");

        var user = tokenEntity.User;

        // Token rotation: revoke cái cũ → cấp mới
        tokenEntity.IsRevoked = true;
        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task LogoutAsync(int userId, string refreshToken, CancellationToken ct = default)
    {
        var token = await db.UserTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.RefreshToken == refreshToken, ct);
        if (token != null)
        {
            token.IsRevoked = true;
            await db.SaveChangesAsync(ct);
        }
    }

    // ────────────────────── PASSWORD RESET ──────────────────────────

    public async Task ForgotPasswordAsync(ForgotPasswordDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return; // không tiết lộ email có tồn tại hay không

        var recent = await db.EmailOtps
            .Where(o => o.Email == email && o.OtpType == "PasswordReset" && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recent != null)
        {
            var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
            if (DateTime.UtcNow - recent.CreatedAt < cooldown)
                throw new InvalidOperationException($"Vui lòng chờ {_emailOpts.OtpResendCooldownSeconds}s.");
        }

        var code = GenerateOtp();
        db.EmailOtps.Add(new EmailOtp
        {
            Email = email,
            OtpCode = code,
            OtpType = "PasswordReset",
            ExpiredAt = DateTime.UtcNow.AddMinutes(_emailOpts.OtpExpiryMinutes)
        });
        await db.SaveChangesAsync(ct);
        await emailService.SendPasswordResetOtpAsync(email, code, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var otp = await db.EmailOtps
            .Where(o => o.Email == email && o.OtpType == "PasswordReset" && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        ValidateOtp(otp, request.OtpCode);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
                   ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        otp!.IsUsed = true;

        // Revoke tất cả refresh token
        var tokens = db.UserTokens.Where(t => t.UserId == user.UserID && !t.IsRevoked);
        await tokens.ForEachAsync(t => t.IsRevoked = true, ct);

        await db.SaveChangesAsync(ct);
    }

    // ────────────────────── HELPERS ────────────────────────────────

    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, CancellationToken ct)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).Distinct().ToList();
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        db.UserTokens.Add(new UserToken
        {
            UserId = user.UserID,
            RefreshToken = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(_jwtOpts.RefreshTokenExpiryDays)
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            expiresAt,
            user.UserID,
            user.Email ?? string.Empty,
            user.FullName,
            roles);
    }

    private static void ValidateOtp(EmailOtp? otp, string code)
    {
        if (otp is null || otp.OtpCode != code)
            throw new InvalidOperationException("Mã OTP không chính xác.");
        if (otp.IsUsed)
            throw new InvalidOperationException("Mã OTP đã được sử dụng.");
        if (otp.ExpiredAt < DateTime.UtcNow)
            throw new InvalidOperationException("Mã OTP đã hết hạn.");
    }

    private static string GenerateOtp()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        var num = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return num.ToString("D6");
    }

    private static string HashPassword(string password)
    {
        const int iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        return $"pbkdf2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
        if (!int.TryParse(parts[1], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations,
                System.Security.Cryptography.HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch { return false; }
    }
}
