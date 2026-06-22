using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class FileStorageService(ILogger<FileStorageService> logger) : IFileStorageService
{
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

    private static string BasePath => Path.Combine(AppContext.BaseDirectory, "wwwroot");

    public async Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string subFolder,
        CancellationToken cancellationToken = default)
    {
        if (stream.Length > MaxFileSizeBytes)
            throw new ArgumentException($"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var uploadPath = Path.Combine(BasePath, "uploads", subFolder);

        Directory.CreateDirectory(uploadPath);

        var fullPath = Path.Combine(uploadPath, safeFileName);
        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, cancellationToken);

        logger.LogInformation("Saved file {FileName} to {RelativePath}", fileName, $"uploads/{subFolder}/{safeFileName}");

        return $"uploads/{subFolder}/{safeFileName}";
    }

    public Task<bool> DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        logger.LogInformation("Deleted file at {RelativePath}", relativePath);
        return Task.FromResult(true);
    }

    public string GetAbsoluteUrl(string relativePath)
    {
        return $"/{relativePath.Replace("\\", "/")}";
    }
}
