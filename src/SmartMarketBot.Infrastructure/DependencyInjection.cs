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

        // Options
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MqttOptions>(configuration.GetSection(MqttOptions.SectionName));
        services.Configure<AiServiceOptions>(configuration.GetSection(AiServiceOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));

        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Auth / Token / Email
        services.AddHttpContextAccessor();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();

        // Domain services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IRobotService, RobotService>();
        services.AddScoped<IRobotRouteService, RobotRouteService>();
        services.AddScoped<IAisleScanService, AisleScanService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IMapSyncService, MapSyncService>();
        services.AddScoped<IZoneAisleService, ZoneAisleService>();
        services.AddScoped<ISemanticObjectService, SemanticObjectService>();

        services.AddScoped<IAdminUserService, AdminUserService>();

        // Member & Staff services
        services.AddScoped<IMealSuggestionService, MealSuggestionService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IMemberNotificationWriter, MemberNotificationWriter>();

        // Brand & Ad Campaign Services
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IAdPackageService, AdPackageService>();
        services.AddScoped<IAdCampaignService, AdCampaignService>();
        services.AddScoped<ISponsoredProductService, SponsoredProductService>();
        services.AddScoped<IAdResourceService, AdResourceService>();
        services.AddScoped<IAdAnalyticsService, AdAnalyticsService>();
        services.AddScoped<IAdRouteService, AdRouteService>();
        services.AddScoped<IAdRecommendationService, AdRecommendationService>();
        services.AddScoped<IGeneralDealService, GeneralDealService>();

        // Realtime (skeleton — 3 bạn fill logic)
        services.AddScoped<IStaffRealtimeService, StaffRealtimeService>();
        services.AddScoped<IMemberRealtimeService, MemberRealtimeService>();

        services.AddHttpClient<IAiVisionProxy, AiVisionProxy>();
        services.AddHttpClient<IFaceAiService, FaceAiService>();
        services.AddHttpClient<IGeminiService, GeminiService>();
        services.AddHttpClient<ICloudStorageService, CloudinaryService>();

        // MQTT (Singleton + HostedService)
        services.AddSingleton<MqttClientService>();
        services.AddSingleton<IRobotCommandPublisher>(sp => sp.GetRequiredService<MqttClientService>());
        services.AddHostedService(sp => sp.GetRequiredService<MqttClientService>());

        return services;
    }
}
