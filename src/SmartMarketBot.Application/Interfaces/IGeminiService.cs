namespace SmartMarketBot.Application.Interfaces;

public interface IGeminiService
{
    /// <summary>Sinh câu chào mừng cá nhân hóa sử dụng Google Gemini</summary>
    Task<string> GeneratePersonalizedGreetingAsync(string fullName, string topProducts, CancellationToken ct = default);
}
