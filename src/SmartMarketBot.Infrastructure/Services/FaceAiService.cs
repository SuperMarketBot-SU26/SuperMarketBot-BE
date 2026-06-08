using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure.Options;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class FaceAiService(
    HttpClient httpClient,
    IOptions<AiServiceOptions> options,
    ILogger<FaceAiService> logger) : IFaceAiService
{
    private readonly AiServiceOptions _options = options.Value;

    public async Task<FaceVerifyResultDto?> VerifyFaceAsync(string imageBase64, CancellationToken ct = default)
    {
        try
        {
            var requestBody = new { image_base64 = imageBase64 };
            var endpoint = new Uri(new Uri(_options.BaseUrl), _options.VerifyFaceEndpoint);

            logger.LogInformation("[FaceAiService] Calling Python face verification at {Url}...", endpoint);

            using var response = await httpClient.PostAsJsonAsync(endpoint, requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[FaceAiService] Python verify returned {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<FaceVerifyResultDto>(cancellationToken: ct);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[FaceAiService] Error calling Python face verification");
            return null;
        }
    }
}
