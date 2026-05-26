using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure.Options;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class AiVisionProxy(
    HttpClient httpClient,
    IOptions<AiServiceOptions> options,
    ILogger<AiVisionProxy> logger) : IAiVisionProxy
{
    private readonly AiServiceOptions _options = options.Value;

    public async Task<string> AnalyzeImageAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageBytes), "file", fileName);

            var endpoint = new Uri(new Uri(_options.BaseUrl), _options.AnalyzeEndpoint);
            using var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("AI vision endpoint returned {StatusCode}", response.StatusCode);
                return BuildMockVisionResult("unavailable");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(body) ? BuildMockVisionResult("empty-response") : body;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI vision endpoint unavailable, returning mock result.");
            return BuildMockVisionResult("exception");
        }
    }

    private static string BuildMockVisionResult(string reason)
    {
        return JsonSerializer.Serialize(new
        {
            source = "mock",
            reason,
            detectedObjects = Array.Empty<object>(),
            emptyPercentage = 0
        });
    }
}
