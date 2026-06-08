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

    public async Task<List<double>?> ExtractFaceVectorAsync(string imageBase64, CancellationToken ct = default)
    {
        try
        {
            var requestBody = new { image_base64 = imageBase64 };
            var endpoint = new Uri(new Uri(_options.BaseUrl), "/extract-vector");

            logger.LogInformation("[FaceAiService] Calling Python face vector extraction at {Url}...", endpoint);

            using var response = await httpClient.PostAsJsonAsync(endpoint, requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[FaceAiService] Python extract-vector returned {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<FaceExtractResponseDto>(cancellationToken: ct);
            return result?.Face_vector;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[FaceAiService] Error calling Python face vector extraction");
            return null;
        }
    }

    private sealed class FaceExtractResponseDto
    {
        public string Status { get; set; } = string.Empty;
        public List<double> Face_vector { get; set; } = new();
    }
}
