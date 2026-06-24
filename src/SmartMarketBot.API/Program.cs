using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.IdentityModel.Tokens;
using SmartMarketBot.API.Hubs;
using SmartMarketBot.API.Middleware;
using SmartMarketBot.API.Realtime;
using SmartMarketBot.Application;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure;
using SmartMarketBot.Infrastructure.Services;
using Scalar.AspNetCore;
using SmartMarketBot.Infrastructure.Options;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new SmartMarketBot.API.Converters.DateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new SmartMarketBot.API.Converters.DateTimeNullableConverter());
    });

// ConfigureHttpJsonOptions affects Minimal APIs AND OpenAPI schema generation (JsonSchemaExporter).
// Without this, DateTime converters are not picked up by the OpenAPI pipeline.
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new SmartMarketBot.API.Converters.DateTimeConverter());
    options.SerializerOptions.Converters.Add(new SmartMarketBot.API.Converters.DateTimeNullableConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IRobotHubNotifier, SignalRRobotHubNotifier>();
builder.Services.AddSingleton<IStaffRealtimeNotifier, SignalRStaffHubNotifier>();
builder.Services.AddSingleton<IMemberRealtimeNotifier, SignalRMemberHubNotifier>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var secretKey = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Scalar API docs — luôn bật (kể cả Production/Azure để demo capstone)
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("SmartMarketBot API");
    options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
});
app.MapGet("/swagger", () => Results.Redirect("/scalar/v1", permanent: false));
app.MapGet("/swagger/index.html", () => Results.Redirect("/scalar/v1", permanent: false));
app.MapGet("/", () => Results.Redirect("/scalar/v1", permanent: false));

app.UseCors("AllowAll");
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RobotHub>("/hubs/robot");
app.MapHub<StaffHub>("/hubs/staff");
app.MapHub<MemberHub>("/hubs/member");

app.Run();
