using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Search;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(ISearchService searchService) : ControllerBase
{
    /// <summary>
    /// Tìm kiếm sản phẩm thông thường, công cộng (không lọc theo cá nhân).
    /// </summary>
    [HttpGet("all")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SearchResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponseDto>> SearchAll(
        [FromQuery] string q = "",
        [FromQuery] int limit = 20,
        [FromQuery] string sortBy = "relevance",
        [FromQuery] bool useAi = false,
        CancellationToken ct = default)
    {
        var result = await searchService.SearchAllAsync(q, limit, sortBy, useAi, ct);
        return Ok(result);
    }

    /// <summary>
    /// Tìm kiếm cá nhân hóa bắt buộc đăng nhập (lọc dị ứng, ngân sách và soft boost chế độ ăn).
    /// </summary>
    [HttpGet("personalized")]
    [Authorize]
    [ProducesResponseType(typeof(SearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SearchResponseDto>> SearchPersonalized(
        [FromQuery] string q = "",
        [FromQuery] int limit = 20,
        [FromQuery] string sortBy = "relevance",
        [FromQuery] bool useAi = false,
        CancellationToken ct = default)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await searchService.SearchPersonalizedAsync(accountId.Value, q, limit, sortBy, useAi, ct);
        return Ok(result);
    }

    /// <summary>
    /// API tương thích ngược (Backward compatibility) cho các client cũ gọi GET /api/search.
    /// Tự động định tuyến sang SearchPersonalized hoặc SearchAll dựa trên tham số memberId.
    /// </summary>
    [HttpGet("")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SearchResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponseDto>> SearchLegacy(
        [FromQuery] string q = "",
        [FromQuery] int? memberId = null,
        [FromQuery] int limit = 20,
        [FromQuery] string sortBy = "relevance",
        [FromQuery] bool useAi = false,
        CancellationToken ct = default)
    {
        if (memberId.HasValue && memberId.Value > 0)
        {
            var result = await searchService.SearchPersonalizedByMemberIdAsync(memberId.Value, q, limit, sortBy, useAi, ct);
            return Ok(result);
        }
        else
        {
            var result = await searchService.SearchAllAsync(q, limit, sortBy, useAi, ct);
            return Ok(result);
        }
    }

    private int? GetCurrentAccountId()
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }
}
