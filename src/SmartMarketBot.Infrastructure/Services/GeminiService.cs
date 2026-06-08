using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure.Options;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class GeminiService(
    HttpClient httpClient,
    IOptions<AiServiceOptions> options,
    ILogger<GeminiService> logger) : IGeminiService
{
    private readonly AiServiceOptions _options = options.Value;

    public async Task<string> GeneratePersonalizedGreetingAsync(string fullName, string topProducts, CancellationToken ct = default)
    {
        var defaultGreeting = $"Chào mừng {fullName} quay trở lại siêu thị!";

        if (string.IsNullOrEmpty(_options.GeminiApiKey))
        {
            logger.LogWarning("[GeminiService] Gemini API Key is missing. Returning default greeting.");
            return defaultGreeting;
        }

        try
        {
            var prompt = $"Bạn là trợ lý ảo siêu thị thông minh. Hãy tạo một câu chào ngắn gọn (dưới 20 từ), thân thiện chào mừng khách hàng tên '{fullName}' quay lại. Biết rằng các sản phẩm họ mua nhiều nhất là '{topProducts}'. Hãy gợi ý nhanh hoặc nhắc tên sản phẩm một cách tinh tế.";
            
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_options.GeminiApiKey}";
            logger.LogInformation("[GeminiService] Calling Gemini API for personalized greeting...");

            using var response = await httpClient.PostAsJsonAsync(url, requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[GeminiService] Gemini API returned {StatusCode}", response.StatusCode);
                return defaultGreeting;
            }

            var responseString = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseString);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text?.Trim() ?? defaultGreeting;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GeminiService] Error calling Gemini API");
            return defaultGreeting;
        }
    }
}
