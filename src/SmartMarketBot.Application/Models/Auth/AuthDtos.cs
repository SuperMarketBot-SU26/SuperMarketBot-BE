namespace SmartMarketBot.Application.Models.Auth;

public sealed record RegisterRequestDto(string Username, string Password, string? Email);

public sealed record LoginRequestDto(string Username, string Password);

public sealed record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    int UserId,
    string Username,
    IReadOnlyList<string> Roles);
