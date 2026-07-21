using System.Net;
using System.Text.Json;
using SmartMarketBot.Application.Interfaces;

namespace SmartMarketBot.API.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ILocalizationService localizer)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized request.");
            await WriteErrorAsync(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found.");
            await WriteErrorAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument.");
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business validation error.");
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            // OpenAPI/Swagger endpoints generate JSON schemas for DTOs using JsonSchemaExporter.
            // This can throw on DateTime/DateTime? properties in .NET 10 due to the numeric
            // roundtrip issue — catch it gracefully so API remains functional.
            bool isSwaggerPath =
                context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/docs", StringComparison.OrdinalIgnoreCase);

            if (isSwaggerPath)
            {
                logger.LogWarning(ex,
                    "OpenAPI/Swagger schema generation threw {ExType}. API endpoints are fully functional.",
                    ex.GetType().Name);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"error\":\"Swagger schema generation failed — all API endpoints remain operational.\"}");
                }
                return;
            }
            logger.LogError(ex, "Unhandled server exception: {ExMessage} | {ExDetails}", ex.Message, ex.ToString());
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, ex.Message);

        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new
        {
            error = message,
            statusCode = (int)statusCode
        });

        await context.Response.WriteAsync(payload);
    }
}
