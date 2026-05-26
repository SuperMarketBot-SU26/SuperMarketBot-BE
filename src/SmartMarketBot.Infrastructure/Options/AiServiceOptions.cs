namespace SmartMarketBot.Infrastructure.Options;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string AnalyzeEndpoint { get; set; } = "/vision/analyze";
}
