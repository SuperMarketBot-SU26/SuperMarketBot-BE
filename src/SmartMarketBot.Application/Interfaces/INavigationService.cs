using SmartMarketBot.Application.Models.Navigation;

namespace SmartMarketBot.Application.Interfaces;

public interface INavigationService
{
    Task<RoutePlanResultDto> PlanRouteAsync(RoutePlanRequestDto request, CancellationToken cancellationToken = default);
}
