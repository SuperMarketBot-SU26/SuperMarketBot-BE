using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Search;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(ISearchService searchService) : ControllerBase
{
    /// <summary>
    /// Tìm kiếm sản phẩm thông minh dựa trên DB khách hàng.
    /// Hỗ trợ lọc theo MemberId (bỏ sản phẩm chứa dị ứng) và AI rerank bằng Gemini.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<SearchResponseDto>> Search(
        [FromQuery] string q = "",
        [FromQuery] int? memberId = null,
        [FromQuery] int limit = 20,
        [FromQuery] string sortBy = "relevance",
        [FromQuery] bool useAi = false,
        CancellationToken ct = default)
    {
        var request = new SearchRequestDto(q, memberId, limit, sortBy, useAi);
        var result = await searchService.SearchAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Phiên bản POST cho trường hợp client gửi JSON body đầy đủ.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<SearchResponseDto>> SearchPost(
        [FromBody] SearchRequestDto request,
        CancellationToken ct = default)
    {
        if (request == null)
            return BadRequest(new { message = "Body không được rỗng." });

        var result = await searchService.SearchAsync(request, ct);
        return Ok(result);
    }
}
