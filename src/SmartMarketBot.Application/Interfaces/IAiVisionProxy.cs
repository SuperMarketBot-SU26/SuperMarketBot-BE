namespace SmartMarketBot.Application.Interfaces;

public interface IAiVisionProxy
{
    Task<string> AnalyzeImageAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken = default);
}
