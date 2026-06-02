using SmartMarketBot.Domain.Entities;

namespace SmartMarketBot.Application.Interfaces;

public interface ITokenService
{
    /// <summary>Tạo Access JWT từ thông tin user + roles</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user, IReadOnlyList<string> roles);

    /// <summary>Tạo Refresh Token ngẫu nhiên (cryptographic)</summary>
    string GenerateRefreshToken();

    /// <summary>Đọc UserId từ expired access token (dùng lúc refresh)</summary>
    int? GetUserIdFromExpiredToken(string token);
}
