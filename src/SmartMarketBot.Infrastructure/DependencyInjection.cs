using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Persistence;
using SmartMarketBot.Infrastructure.Services;

namespace SmartMarketBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=SmartMarketBot;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MqttOptions>(configuration.GetSection(MqttOptions.SectionName));
        services.Configure<AiServiceOptions>(configuration.GetSection(AiServiceOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IRobotService, RobotService>();
        services.AddScoped<IShelfScanService, ShelfScanService>();

        services.AddHttpClient<IAiVisionProxy, AiVisionProxy>();

        services.AddSingleton<MqttClientService>();
        services.AddSingleton<IRobotCommandPublisher>(sp => sp.GetRequiredService<MqttClientService>());
        services.AddHostedService(sp => sp.GetRequiredService<MqttClientService>());

        return services;
    }
}
