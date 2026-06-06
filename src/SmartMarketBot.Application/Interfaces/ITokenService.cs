using SmartMarketBot.Domain.Entities;

namespace SmartMarketBot.Application.Interfaces;

public interface ITokenService
{
    /// <summary>Tạo Access JWT từ thông tin Account + role string</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(Account account, IReadOnlyList<string> roles);

    /// <summary>Tạo Refresh Token ngẫu nhiên (cryptographic)</summary>
    string GenerateRefreshToken();

    /// <summary>Đọc AccountId từ expired access token (dùng lúc refresh)</summary>
    int? GetUserIdFromExpiredToken(string token);
}
