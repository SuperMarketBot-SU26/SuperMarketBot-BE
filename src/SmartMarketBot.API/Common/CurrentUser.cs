using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SmartMarketBot.API.Common;

/// <summary>
/// Helper cho Controller để lấy AccountId từ JWT claims một cách nhất quán.
/// </summary>
public static class CurrentUser
{
    /// <summary>Lấy AccountId từ JWT sub claim. Trả về null nếu không parse được.</summary>
    public static int? GetAccountId(ClaimsPrincipal? user)
    {
        if (user is null) return null;

        var sub = user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>Helper trả về Unauthorized() nếu không có AccountId hợp lệ.</summary>
    public static ActionResult? EnsureAccountId(ClaimsPrincipal? user, out int accountId)
    {
        accountId = 0;
        var id = GetAccountId(user);
        if (id is null) return new UnauthorizedResult();
        accountId = id.Value;
        return null;
    }
}