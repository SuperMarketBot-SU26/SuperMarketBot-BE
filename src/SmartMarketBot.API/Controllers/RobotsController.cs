using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Robots;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RobotsController(IRobotService robotService) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 1, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IReadOnlyList<RobotDto>>> GetRobots(CancellationToken cancellationToken)
    {
        var robots = await robotService.GetRobotsAsync(cancellationToken);
        return Ok(robots);
    }

    /// <summary>
    /// Trả về danh sách trạng thái hợp lệ của robot (enum string).
    /// </summary>
    [HttpGet("status-values")]
    public IActionResult GetStatusValues()
    {
        var values = new[]
        {
            "Power_Off",
            "Idle",
            "Moving",
            "Interacting",
            "Offline_Charging"
        };
        return Ok(values);
    }

    [HttpPost("command")]
    public async Task<IActionResult> PublishCommand([FromBody] PublishRobotCommandRequestDto request, CancellationToken cancellationToken)
    {
        await robotService.PublishCommandAsync(request, cancellationToken);
        return Accepted();
    }

    /// <summary>
    /// Tính route (Dijkstra) rồi publish lệnh navigate xuống robot qua MQTT.
    /// </summary>
    [HttpPost("navigate")]
    public async Task<IActionResult> Navigate([FromBody] NavigateRobotRequestDto request, CancellationToken cancellationToken)
    {
        await robotService.NavigateRobotAsync(request, cancellationToken);
        return Accepted(new { message = $"Navigate command sent to robot {request.RobotCode}." });
    }

    /// <summary>
    /// Lấy pose mới nhất của robot (Dead Reckoning — x, y, heading).
    /// </summary>
    [HttpGet("{robotCode}/pose")]
    [ResponseCache(Duration = 1, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<RobotPoseDto>> GetPose(string robotCode, CancellationToken cancellationToken)
    {
        var pose = await robotService.GetPoseAsync(robotCode, cancellationToken);
        return Ok(pose);
    }


    /// <summary>
    /// Cập nhật trạng thái robot (Power_Off | Idle | Moving | Interacting | Offline_Charging).
    /// Robot firmware (ESP32-S3) nên gọi endpoint này mỗi lần đổi trạng thái.
    /// BE sẽ validate, lưu DB, ghi Robot_Logs và broadcast SignalR.
    /// </summary>
    [HttpPost("{robotCode}/status")]
    public async Task<ActionResult<RobotDto>> UpdateStatus(
        string robotCode,
        [FromBody] UpdateRobotStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { message = "Body không được rỗng." });
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new
            {
                message = "Status bắt buộc. Hợp lệ: Power_Off, Idle, Moving, Interacting, Offline_Charging."
            });

        try
        {
            var updated = await robotService.UpdateStatusAsync(robotCode, request, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
