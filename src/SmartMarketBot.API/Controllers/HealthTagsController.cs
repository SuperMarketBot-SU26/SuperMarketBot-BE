using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Members;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Danh mục HealthTag dùng cho UI picker (chế độ ăn / dị ứng / thành phần tránh).
/// </summary>
[ApiController]
[Route("api/health-tags")]
public sealed class HealthTagsController(IMemberService memberService) : ControllerBase
{
    /// <summary>Lấy toàn bộ HealthTag trong hệ thống, nhóm theo TagType.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<HealthTagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HealthTagDto>>> GetAll(CancellationToken cancellationToken)
    {
        var tags = await memberService.GetAllHealthTagsAsync(cancellationToken);
        return Ok(tags);
    }
}
