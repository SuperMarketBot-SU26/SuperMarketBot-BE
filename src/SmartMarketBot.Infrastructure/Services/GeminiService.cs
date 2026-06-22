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

    public async Task<string> RerankAndExplainAsync(string query, IReadOnlyList<string> productIdAndNames, CancellationToken ct = default)
    {
        // Fallback: nếu không có API key hoặc lỗi → trả nguyên thứ tự ban đầu
        var fallback = string.Join(",", productIdAndNames.Select(s => s.Split(':')[0]));

        if (string.IsNullOrEmpty(_options.GeminiApiKey) || productIdAndNames.Count == 0)
            return fallback;

        try
        {
            var listStr = string.Join("\n", productIdAndNames.Select((p, i) => $"{i + 1}. {p}"));
            var prompt =
                $"Bạn là hệ thống tìm kiếm sản phẩm siêu thị thông minh.\n" +
                $"Người dùng tìm kiếm với từ khóa: \"{query}\"\n\n" +
                $"Danh sách sản phẩm (id:tên):\n{listStr}\n\n" +
                $"Hãy sắp xếp lại danh sách trên theo mức độ phù hợp giảm dần với từ khóa.\n" +
                $"CHỈ trả về danh sách id theo thứ tự, phân tách bằng dấu phẩy, KHÔNG kèm giải thích.\n" +
                $"Ví dụ: 12,5,8,1";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_options.GeminiApiKey}";
            using var response = await httpClient.PostAsJsonAsync(url, requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[GeminiService] Rerank returned {StatusCode}", response.StatusCode);
                return fallback;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            var cleaned = (text ?? fallback).Trim();
            // Validate: chỉ giữ số và dấu phẩy
            var valid = string.Join(",",
                cleaned.Split(new[] { ',', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => int.TryParse(s.Trim(), out _)));

            return string.IsNullOrEmpty(valid) ? fallback : valid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GeminiService] Rerank failed");
            return fallback;
        }
    }
}
