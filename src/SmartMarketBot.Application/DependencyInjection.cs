using Microsoft.Extensions.DependencyInjection;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Services;

namespace SmartMarketBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<NavigationCommandService>();
        // IAuthService, IEmailService, ITokenService, ISePayService đăng ký trong Infrastructure
        return services;
    }
}
