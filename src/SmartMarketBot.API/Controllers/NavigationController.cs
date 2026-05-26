using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NavigationController(INavigationService navigationService) : ControllerBase
{
    [HttpPost("route")]
    public async Task<ActionResult<RoutePlanResultDto>> PlanRoute([FromBody] RoutePlanRequestDto request, CancellationToken cancellationToken)
    {
        var route = await navigationService.PlanRouteAsync(request, cancellationToken);
        return Ok(route);
    }
}
