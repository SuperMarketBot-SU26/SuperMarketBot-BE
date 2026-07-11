using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/ad-routes")]
public sealed class AdRoutesController(
    IAdRouteService adRouteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AdRouteResponseDto>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await adRouteService.GetListAsync(pageNumber, pageSize, isActive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{routeId:int}")]
    public async Task<ActionResult<AdRouteResponseDto>> GetById(int routeId, CancellationToken cancellationToken)
    {
        var route = await adRouteService.GetByIdAsync(routeId, cancellationToken);
        if (route == null)
            return NotFound();
        return Ok(route);
    }

    [HttpGet("robot/{robotId:int}/active")]
    public async Task<ActionResult<AdRouteResponseDto>> GetActiveRouteForRobot(
        int robotId, CancellationToken cancellationToken)
    {
        var route = await adRouteService.GetActiveRouteForRobotAsync(robotId, cancellationToken);
        if (route == null)
            return NotFound(new { message = "No active route assigned to this robot." });
        return Ok(route);
    }

    [HttpPost]
    public async Task<ActionResult<AdRouteResponseDto>> Create(
        [FromBody] CreateAdRouteRequestDto request,
        CancellationToken cancellationToken)
    {
        var route = await adRouteService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { routeId = route.AdRouteId }, route);
    }

    [HttpPut("{routeId:int}")]
    public async Task<ActionResult<AdRouteResponseDto>> Update(
        int routeId,
        [FromBody] UpdateAdRouteRequestDto request,
        CancellationToken cancellationToken)
    {
        var route = await adRouteService.UpdateAsync(routeId, request, cancellationToken);
        return Ok(route);
    }

    [HttpDelete("{routeId:int}")]
    public async Task<ActionResult> Delete(int routeId, CancellationToken cancellationToken)
    {
        await adRouteService.DeleteAsync(routeId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{routeId:int}/assign/{robotId:int}")]
    public async Task<ActionResult<AdRouteResponseDto>> AssignToRobot(
        int routeId, int robotId, CancellationToken cancellationToken)
    {
        var route = await adRouteService.AssignToRobotAsync(routeId, robotId, cancellationToken);
        return Ok(route);
    }
}
