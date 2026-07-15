using System.Security.Cryptography;
using System.Text.Json;
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
    ICloudStorageService cloudStorage,
    IOptions<CloudinaryOptions> cloudinaryOptions,
    ILogger<AuthService> logger,
    ILocalizationService localizer) : IAuthService
{
    private readonly EmailOptions _emailOpts = emailOptions.Value;
    private readonly JwtOptions _jwtOpts = jwtOptions.Value;
    private readonly CloudinaryOptions _cloudinaryOpts = cloudinaryOptions.Value;

    private const string OtpTypeRegistration = "Registration";
    private const string OtpTypePasswordReset = "PasswordReset";

    // ────────────────────── REGISTER FLOW ──────────────────────────

    public async Task RegisterRequestOtpAsync(RegisterRequestOtpDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existingAccount = await db.Accounts.AnyAsync(a => a.Email == email, ct);
        if (existingAccount)
            throw new InvalidOperationException(localizer.Get("EmailInUse"));

        // Lưu tạm thông tin đăng ký + OTP vào Account. Kích hoạt Active luôn để hỗ trợ Mobile gọi login trực tiếp.
        // Schema mới (ERD V4.0) gộp EMAIL_OTP vào ACCOUNT - không còn bảng riêng.
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct);
        var otpCode = GenerateOtp();

        if (account == null)
        {
            account = new Account
            {
                Username = email,
                Email = email,
                PasswordHash = HashPassword(request.Password),
                FullName = request.FullName,
                Phone = request.Phone,
                Status = "Active",
                Role = AccountRole.Member.ToString(),
                OtpCode = otpCode,
                OtpType = OtpTypeRegistration,
                OtpExpiredAt = VnDateTime.Now.AddMinutes(_emailOpts.OtpExpiryMinutes)
            };
            db.Accounts.Add(account);
        }
        else
        {
            // Cooldown check
            if (account.OtpCode != null && account.OtpType == OtpTypeRegistration)
            {
                var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
                if (VnDateTime.Now - account.CreatedAt < cooldown)
                    throw new InvalidOperationException(localizer.Get("OtpResendCooldown", _emailOpts.OtpResendCooldownSeconds));
            }

            account.PasswordHash = HashPassword(request.Password);
            account.FullName = request.FullName;
            account.Phone = request.Phone;
            account.Status = "Active";
            account.OtpCode = otpCode;
            account.OtpType = OtpTypeRegistration;
            account.OtpExpiredAt = VnDateTime.Now.AddMinutes(_emailOpts.OtpExpiryMinutes);
        }

        // Tự động tạo Member nếu là vai trò Member và chưa tồn tại
        if (account.Role == AccountRole.Member.ToString())
        {
            var exists = await db.Members.AnyAsync(m => m.AccountId == account.AccountId || m.Account == account, ct);
            if (!exists)
            {
                var member = new Member
                {
                    Account = account,
                    FullName = account.FullName ?? account.Email,
                    TotalPoints = 0
                };
                db.Members.Add(member);
            }
        }

        await db.SaveChangesAsync(ct);
        await emailService.SendRegistrationOtpAsync(email, request.FullName, otpCode, ct);
        logger.LogInformation("[Auth] Registration completed. Account set to Active, Member record created and OTP sent → {Email}", email);
    }

    public async Task<AuthResponseDto> VerifyOtpAndRegisterAsync(VerifyOtpDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct);
        ValidateAccountOtp(account, request.OtpCode, OtpTypeRegistration);

        // Kích hoạt tài khoản
        account!.Status = "Active";
        account.OtpCode = null;
        account.OtpType = null;
        account.OtpExpiredAt = null;

        // Tự động tạo Member nếu là vai trò Member và chưa tồn tại
        if (account.Role == AccountRole.Member.ToString())
        {
            var exists = await db.Members.AnyAsync(m => m.AccountId == account.AccountId, ct);
            if (!exists)
            {
                var member = new Member
                {
                    AccountId = account.AccountId,
                    FullName = account.FullName ?? account.Email,
                    TotalPoints = 0
                };
                db.Members.Add(member);
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Auth] Account registered and Member record created: {Email}", email);
        return await BuildAuthResponseAsync(account, ct);
    }

    public async Task ResendOtpAsync(ResendOtpDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct)
                   ?? throw new InvalidOperationException(localizer.Get("OtpRequestNotFound"));

        if (account.OtpCode == null)
            throw new InvalidOperationException(localizer.Get("OtpRequestNotFound"));

        var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
        if (VnDateTime.Now - account.CreatedAt < cooldown)
            throw new InvalidOperationException(localizer.Get("OtpResendCooldown", _emailOpts.OtpResendCooldownSeconds));

        var newCode = GenerateOtp();
        account.OtpCode = newCode;
        account.OtpExpiredAt = VnDateTime.Now.AddMinutes(_emailOpts.OtpExpiryMinutes);

        await db.SaveChangesAsync(ct);

        if (account.OtpType == OtpTypePasswordReset)
            await emailService.SendPasswordResetOtpAsync(email, newCode, ct);
        else
            await emailService.SendRegistrationOtpAsync(email, account.FullName ?? email, newCode, ct);
    }

    // ────────────────────── LOGIN / TOKEN ──────────────────────────

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Email == email, ct);

        if (account is null || !VerifyPassword(request.Password, account.PasswordHash))
            throw new UnauthorizedAccessException(localizer.Get("LoginInvalid"));

        if (account.Status != "Active")
            throw new UnauthorizedAccessException(localizer.Get("AccountLocked"));

        return await BuildAuthResponseAsync(account, ct);
    }

    public async Task<FaceLoginResponseDto> FaceLoginAsync(FaceLoginRequestDto request, CancellationToken ct = default)
    {
        // Strategy A: gọi Python AI service (chính xác cao, dùng Cloudinary Face API nếu có)
        var verifyResult = await faceAiService.VerifyFaceAsync(request.ImageBase64, ct);

        Member? member = null;
        double confidence = 0;
        if (verifyResult is { Status: "success" } && verifyResult.MemberId > 0)
        {
            member = await db.Members
                .Include(m => m.Account)
                .FirstOrDefaultAsync(m => m.MemberId == verifyResult.MemberId, ct);
            confidence = verifyResult.ConfidenceScore;
        }

        // Strategy B: nếu Python không xác thực được → tự trích vector rồi so khớp với tất cả FaceVector trong DB
        // (cosine similarity — fallback khi Python AI down)
        if (member == null)
        {
            var probeVector = await faceAiService.ExtractFaceVectorAsync(request.ImageBase64, ct);
            if (probeVector != null && probeVector.Count > 0)
            {
                var best = await FindBestFaceMatchAsync(probeVector, ct);
                if (best is not null)
                {
                    member = best.Value.Member;
                    confidence = best.Value.Similarity;
                    logger.LogInformation(
                        "[AuthService] Face matched via DB vector scan → MemberId={Id}, sim={Sim:F3}",
                        member.MemberId, confidence);
                }
            }
        }

        if (member == null)
        {
            return new FaceLoginResponseDto(false, "Không nhận diện được khuôn mặt", null, null, null);
        }

        // 3. Truy vấn các sản phẩm mua nhiều nhất từ lịch sử mua hàng
        var topProductsList = await db.InvoiceHistoryItems
            .Where(i => i.InvoiceHistory!.MemberId == member.MemberId)
            .GroupBy(i => i.Product!.ProductName)
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

        var memberTier = await db.Memberships
            .Where(mp => mp.MemberId == member.MemberId)
            .OrderByDescending(mp => mp.MembershipId)
            .Select(mp => mp.TierName)
            .FirstOrDefaultAsync(ct);

        var memberDto = new FaceLoginMemberDto(
            member.MemberId,
            member.FullName,
            member.Account?.Phone ?? "",
            memberTier ?? "Bronze",
            member.TotalPoints
        );

        return new FaceLoginResponseDto(
            true,
            $"Đăng nhập bằng khuôn mặt thành công (confidence: {confidence:F2})",
            greeting, tokenDto, memberDto);
    }

    /// <summary>
    /// Duyệt tất cả Member có FaceVector trong DB, tính cosine similarity với vector đầu vào.
    /// Ngưỡng chấp nhận: 0.85. Trả về match tốt nhất nếu vượt ngưỡng.
    /// </summary>
    private async Task<(Member Member, double Similarity)?> FindBestFaceMatchAsync(
        List<double> probe, CancellationToken ct)
    {
        const double Threshold = 0.60;

        var members = await db.Members
            .Include(m => m.Account)
            .Where(m => m.FaceVector != null && m.FaceVector != "")
            .ToListAsync(ct);

        Member? bestMember = null;
        double bestSim = 0;

        foreach (var m in members)
        {
            try
            {
                var stored = JsonSerializer.Deserialize<List<double>>(m.FaceVector!);
                if (stored is null || stored.Count == 0 || stored.Count != probe.Count)
                    continue;

                var sim = CosineSimilarity(probe, stored);
                if (sim > bestSim)
                {
                    bestSim = sim;
                    bestMember = m;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[AuthService] Bad FaceVector JSON for MemberId={Id}", m.MemberId);
            }
        }

        if (bestMember != null && bestSim >= Threshold)
            return (bestMember, bestSim);

        return null;
    }

    private static double CosineSimilarity(List<double> a, List<double> b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    public async Task<bool> RegisterFaceAsync(int userId, FaceLoginRequestDto request, CancellationToken ct = default)
    {
        // 1. Tìm Member gắn với AccountId này
        var member = await db.Members
            .FirstOrDefaultAsync(m => m.AccountId == userId, ct);

        if (member == null)
        {
            logger.LogWarning("[AuthService] Không tìm thấy Member cho AccountId: {UserId}", userId);
            return false;
        }

        // 2. Gọi Python AI trích xuất vector khuôn mặt
        var vector = await faceAiService.ExtractFaceVectorAsync(request.ImageBase64, ct);
        if (vector == null || vector.Count == 0)
        {
            logger.LogWarning("[AuthService] Không trích xuất được vector khuôn mặt cho MemberId: {MemberId}", member.MemberId);
            return false;
        }

        // 3. Upload ảnh lên Cloudinary (fallback local nếu chưa config)
        try
        {
            var fileName = $"member-{member.MemberId}-{Guid.NewGuid():N}";
            var imageUrl = await cloudStorage.UploadBase64Async(
                request.ImageBase64,
                _cloudinaryOpts.MemberFacesFolder,
                fileName,
                ct);

            // 4. Cập nhật thông tin FacePath (Cloudinary URL) và FaceVector trong Database
            member.FacePath = imageUrl;
            member.FaceVector = JsonSerializer.Serialize(vector);

            db.Members.Update(member);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("[AuthService] Đăng ký khuôn mặt thành công cho MemberId: {MemberId}. ImageUrl: {Url}", member.MemberId, imageUrl);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AuthService] Lỗi khi upload ảnh hoặc cập nhật Database đăng ký khuôn mặt");
            return false;
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.RefreshToken == request.RefreshToken
                                   && !a.IsTokenRevoked
                                   && a.RefreshExpiry > VnDateTime.Now, ct);

        if (account is null)
            throw new UnauthorizedAccessException(localizer.Get("RefreshTokenInvalid"));

        account.IsTokenRevoked = true; // Rotation: revoke token cũ trước khi cấp mới
        return await BuildAuthResponseAsync(account, ct);
    }

    public async Task LogoutAsync(int userId, string refreshToken, CancellationToken ct = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == userId && a.RefreshToken == refreshToken, ct);
        if (account != null)
        {
            account.RefreshToken = null;
            account.RefreshExpiry = null;
            account.IsTokenRevoked = true;
            await db.SaveChangesAsync(ct);
        }
    }

    // ────────────────────── PASSWORD RESET ──────────────────────────

    public async Task ForgotPasswordAsync(ForgotPasswordDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct);
        if (account is null) return; // không tiết lộ email có tồn tại hay không

        var cooldown = TimeSpan.FromSeconds(_emailOpts.OtpResendCooldownSeconds);
        if (account.OtpCode != null && VnDateTime.Now - account.CreatedAt < cooldown)
            throw new InvalidOperationException(localizer.Get("OtpResendCooldown", _emailOpts.OtpResendCooldownSeconds));

        var code = GenerateOtp();
        account.OtpCode = code;
        account.OtpType = OtpTypePasswordReset;
        account.OtpExpiredAt = VnDateTime.Now.AddMinutes(_emailOpts.OtpExpiryMinutes);

        await db.SaveChangesAsync(ct);
        await emailService.SendPasswordResetOtpAsync(email, code, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct)
                   ?? throw new KeyNotFoundException(localizer.Get("AccountNotFound"));

        ValidateAccountOtp(account, request.OtpCode, OtpTypePasswordReset);

        account.PasswordHash = HashPassword(request.NewPassword);
        account.OtpCode = null;
        account.OtpType = null;
        account.OtpExpiredAt = null;

        // Revoke refresh token hiện tại (chỉ 1 token/Account)
        account.RefreshToken = null;
        account.RefreshExpiry = null;
        account.IsTokenRevoked = true;

        await db.SaveChangesAsync(ct);
    }

    // ────────────────────── HELPERS ────────────────────────────────

    private async Task<AuthResponseDto> BuildAuthResponseAsync(Account account, CancellationToken ct)
    {
        var roles = new List<string> { account.Role };
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(account, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        // Refresh token giữ trực tiếp trên Account (1 token/Account, revoke = 1 flag).
        account.RefreshToken = refreshToken;
        account.RefreshExpiry = VnDateTime.Now.AddDays(_jwtOpts.RefreshTokenExpiryDays);
        account.IsTokenRevoked = false;
        await db.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            expiresAt,
            account.AccountId,
            account.Email,
            account.FullName,
            roles);
    }

    /// <summary>
    /// Validate OTP dựa trên các trường OtpCode/OtpExpiredAt/OtpType trên Account
    /// (sau khi ERD V4.0 gộp EMAIL_OTP vào ACCOUNT).
    /// </summary>
    private void ValidateAccountOtp(Account? account, string code, string expectedType)
    {
        if (account is null || account.OtpCode == null || account.OtpCode != code)
            throw new InvalidOperationException(localizer.Get("OtpInvalid"));
        if (account.OtpType != expectedType)
            throw new InvalidOperationException(localizer.Get("OtpInvalid"));
        if (account.OtpExpiredAt < VnDateTime.Now)
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
