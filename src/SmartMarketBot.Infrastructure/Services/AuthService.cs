using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Auth;
using SmartMarketBot.Domain.Common;
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
    IFaceAiService faceAiService,
    IGeminiService geminiService,
    ILogger<AuthService> logger,
    ILocalizationService localizer) : IAuthService
{
    private readonly EmailOptions _emailOpts = emailOptions.Value;
    private readonly JwtOptions _jwtOpts = jwtOptions.Value;

    // ────────────────────── REGISTER FLOW ──────────────────────────

    public async Task RegisterRequestOtpAsync(RegisterRequestOtpDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existingAccount = await db.Accounts.AnyAsync(a => a.Email == email, ct);
        if (existingAccount)
            throw new InvalidOperationException(localizer.Get("EmailInUse"));

        var recentOtp = await db.EmailOtps
            .Where(o => o.Email == email && o.OtpType == "Registration" && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recentOtp != null)
        {
            var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
            if (VnDateTime.Now - recentOtp.CreatedAt < cooldown)
                throw new InvalidOperationException(localizer.Get("OtpResendCooldown", _emailOpts.OtpResendCooldownSeconds));
        }

        var otpCode = GenerateOtp();
        var otp = new EmailOtp
        {
            Email = email,
            OtpCode = otpCode,
            OtpType = "Registration",
            ExpiredAt = VnDateTime.Now.AddMinutes(_emailOpts.OtpExpiryMinutes),
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

        var account = new Account
        {
            Username = email,
            Email = email,
            PasswordHash = otp!.TemporaryPasswordHash!,
            FullName = otp.TemporaryFullName,
            Phone = otp.TemporaryPhone,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = VnDateTime.Now,
            Role = AccountRole.Member
        };

        otp.IsUsed = true;
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Auth] Account registered: {Email}", email);
        return await BuildAuthResponseAsync(account, ct);
    }

    public async Task ResendOtpAsync(ResendOtpDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var lastOtp = await db.EmailOtps
            .Where(o => o.Email == email && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (lastOtp == null)
            throw new InvalidOperationException(localizer.Get("OtpRequestNotFound"));

        var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
        if (VnDateTime.Now - lastOtp.CreatedAt < cooldown)
            throw new InvalidOperationException(localizer.Get("OtpResendCooldown", _emailOpts.OtpResendCooldownSeconds));

        lastOtp.IsUsed = true;
        var newCode = GenerateOtp();
        var newOtp = new EmailOtp
        {
            Email = email,
            OtpCode = newCode,
            OtpType = lastOtp.OtpType,
            ExpiredAt = VnDateTime.Now.AddMinutes(_emailOpts.OtpExpiryMinutes),
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

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Email == email, ct);

        if (account is null || !VerifyPassword(request.Password, account.PasswordHash))
            throw new UnauthorizedAccessException(localizer.Get("LoginInvalid"));

        if (!account.IsActive)
            throw new UnauthorizedAccessException(localizer.Get("AccountLocked"));

        if (!account.EmailConfirmed)
            throw new UnauthorizedAccessException(localizer.Get("EmailNotConfirmed"));

        return await BuildAuthResponseAsync(account, ct);
    }

    public async Task<FaceLoginResponseDto> FaceLoginAsync(FaceLoginRequestDto request, CancellationToken ct = default)
    {
        // 1. Gọi Python service để xác thực khuôn mặt từ Base64
        var verifyResult = await faceAiService.VerifyFaceAsync(request.ImageBase64, ct);
        if (verifyResult == null || verifyResult.Status != "success")
        {
            return new FaceLoginResponseDto(false, "Không nhận diện được khuôn mặt", null, null, null);
        }

        // 2. Tìm thông tin Member trong DB
        var member = await db.Members
            .Include(m => m.Account)
            .FirstOrDefaultAsync(m => m.MemberID == verifyResult.MemberId, ct);

        if (member == null)
        {
            return new FaceLoginResponseDto(false, "Không tìm thấy thông tin hội viên", null, null, null);
        }

        // 3. Truy vấn các sản phẩm mua nhiều nhất từ lịch sử mua hàng
        var topProductsList = await db.HistoryItems
            .Where(i => i.ShoppingHistory.MemberID == member.MemberID)
            .GroupBy(i => i.Product.ProductName)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(3)
            .ToListAsync(ct);

        var topProductsStr = topProductsList.Any()
            ? string.Join(", ", topProductsList)
            : "chưa có lịch sử mua hàng";

        // 4. Gọi Gemini API để sinh câu chào cá nhân hóa
        var greeting = await geminiService.GeneratePersonalizedGreetingAsync(member.FullName, topProductsStr, ct);

        // 5. Nếu thành viên có tài khoản liên kết, tự động sinh JWT Token đăng nhập
        AuthResponseDto? tokenDto = null;
        if (member.Account != null)
        {
            tokenDto = await BuildAuthResponseAsync(member.Account, ct);
        }

        var memberDto = new FaceLoginMemberDto(
            member.MemberID,
            member.FullName,
            member.PhoneNumber,
            member.Tier,
            member.TotalPoints
        );

        return new FaceLoginResponseDto(true, "Đăng nhập bằng khuôn mặt thành công", greeting, tokenDto, memberDto);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default)
    {
        var tokenEntity = await db.UserTokens
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.RefreshToken == request.RefreshToken && !t.IsRevoked, ct);

        if (tokenEntity is null || tokenEntity.ExpiryDate < VnDateTime.Now)
            throw new UnauthorizedAccessException(localizer.Get("RefreshTokenInvalid"));

        var account = tokenEntity.Account;

        tokenEntity.IsRevoked = true;
        return await BuildAuthResponseAsync(account, ct);
    }

    public async Task LogoutAsync(int userId, string refreshToken, CancellationToken ct = default)
    {
        var token = await db.UserTokens
            .FirstOrDefaultAsync(t => t.AccountId == userId && t.RefreshToken == refreshToken, ct);
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

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct);
        if (account is null) return; // không tiết lộ email có tồn tại hay không

        var recent = await db.EmailOtps
            .Where(o => o.Email == email && o.OtpType == "PasswordReset" && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recent != null)
        {
            var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
            if (VnDateTime.Now - recent.CreatedAt < cooldown)
                throw new InvalidOperationException(localizer.Get("OtpResendCooldown", _emailOpts.OtpResendCooldownSeconds));
        }

        var code = GenerateOtp();
        db.EmailOtps.Add(new EmailOtp
        {
            Email = email,
            OtpCode = code,
            OtpType = "PasswordReset",
            ExpiredAt = VnDateTime.Now.AddMinutes(_emailOpts.OtpExpiryMinutes)
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

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct)
                   ?? throw new KeyNotFoundException(localizer.Get("AccountNotFound"));

        account.PasswordHash = HashPassword(request.NewPassword);
        account.UpdatedAt = VnDateTime.Now;
        otp!.IsUsed = true;

        // Revoke tất cả refresh token của account này
        var tokens = db.UserTokens.Where(t => t.AccountId == account.AccountID && !t.IsRevoked);
        await tokens.ForEachAsync(t => t.IsRevoked = true, ct);

        await db.SaveChangesAsync(ct);
    }

    // ────────────────────── HELPERS ────────────────────────────────

    private async Task<AuthResponseDto> BuildAuthResponseAsync(Account account, CancellationToken ct)
    {
        var roles = new List<string> { account.Role.ToString() };
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(account, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        db.UserTokens.Add(new UserToken
        {
            AccountId = account.AccountID,
            RefreshToken = refreshToken,
            ExpiryDate = VnDateTime.Now.AddDays(_jwtOpts.RefreshTokenExpiryDays)
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            expiresAt,
            account.AccountID,
            account.Email ?? string.Empty,
            account.FullName,
            roles);
    }

    private void ValidateOtp(EmailOtp? otp, string code)
    {
        if (otp is null || otp.OtpCode != code)
            throw new InvalidOperationException(localizer.Get("OtpInvalid"));
        if (otp.IsUsed)
            throw new InvalidOperationException(localizer.Get("OtpUsed"));
        if (otp.ExpiredAt < VnDateTime.Now)
            throw new InvalidOperationException(localizer.Get("OtpExpired"));
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
