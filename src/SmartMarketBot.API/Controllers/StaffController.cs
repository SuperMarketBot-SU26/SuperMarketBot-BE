using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Staff;

namespace SmartMarketBot.API.Controllers;

/// <summary>Flow 4 — Out-of-Stock Handler: quản lý nhiệm vụ tiếp hàng (Staff App).</summary>
[ApiController]
[Route("api/staff")]
public sealed class StaffController(IStaffService staffService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách nhiệm vụ bổ sung hàng đang chờ xử lý.
    /// Trả về vị trí kệ chi tiết 5 tầng (Zone-Aisle-Level-Slot), tên sản phẩm, số lượng cần bổ sung.
    /// </summary>
    [HttpGet("tasks")]
    [AllowAnonymous]
    public async Task<ActionResult<RestockTaskListResponseDto>> GetRestockTasks(CancellationToken cancellationToken)
    {
        var result = await staffService.GetRestockTasksAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Nhân viên xác nhận đã bổ sung hàng xong.
    /// Backend cập nhật Slot.Quantity và đóng nhiệm vụ.
    /// </summary>
    [HttpPost("tasks/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteRestock(
        [FromBody] CompleteRestockRequestDto request,
        CancellationToken cancellationToken)
    {
        await staffService.CompleteRestockAsync(request, cancellationToken);
        return NoContent();
    }
}
