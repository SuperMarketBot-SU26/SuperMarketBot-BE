using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Robots;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RobotsController(IRobotService robotService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RobotDto>>> GetRobots(CancellationToken cancellationToken)
    {
        var robots = await robotService.GetRobotsAsync(cancellationToken);
        return Ok(robots);
    }

    [HttpPost("command")]
    public async Task<IActionResult> PublishCommand([FromBody] PublishRobotCommandRequestDto request, CancellationToken cancellationToken)
    {
        await robotService.PublishCommandAsync(request, cancellationToken);
        return Accepted();
    }
}
