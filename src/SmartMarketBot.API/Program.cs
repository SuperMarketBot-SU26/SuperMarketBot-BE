using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartMarketBot.API.Hubs;
using SmartMarketBot.API.Middleware;
using SmartMarketBot.API.Realtime;
using SmartMarketBot.Application;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure;
using Scalar.AspNetCore;
using SmartMarketBot.Infrastructure.Options;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
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

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RobotHub>("/hubs/robot");
app.MapHub<StaffHub>("/hubs/staff");
app.MapHub<MemberHub>("/hubs/member");

app.Run();
