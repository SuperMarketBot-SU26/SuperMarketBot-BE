using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure.Options;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class SupabaseFileStorageService(
    IOptions<SupabaseOptions> options,
    ILogger<SupabaseFileStorageService> logger) : IFileStorageService
{
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

    private SupabaseOptions Config => options.Value;

    private Supabase.Client? _client;

    private Supabase.Client Client
    {
        get
        {
            if (_client is null)
            {
                var opts = new Supabase.SupabaseOptions { AutoConnectRealtime = false };
                _client = new Supabase.Client(Config.Url, Config.ApiKey, opts);
            }
            return _client;
        }
    }

    public async Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string subFolder,
        CancellationToken cancellationToken = default)
    {
        if (stream.Length > MaxFileSizeBytes)
            throw new ArgumentException($"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (string.IsNullOrWhiteSpace(Config.Url) || string.IsNullOrWhiteSpace(Config.ApiKey))
            throw new InvalidOperationException(
                "Supabase is not configured. Please set Supabase:Url and Supabase:ApiKey in appsettings.json.");

        await Client.InitializeAsync();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var storagePath = string.IsNullOrWhiteSpace(subFolder)
            ? safeFileName
            : $"{subFolder.TrimStart('/')}/{safeFileName}";

        var bucket = Client.Storage.From(Config.StorageBucket);

        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        await bucket.Upload(memoryStream.ToArray(), storagePath);

        var publicUrl = GetPublicUrl(storagePath);

        logger.LogInformation(
            "Uploaded file {FileName} to Supabase bucket '{Bucket}' at {StoragePath}",
            fileName, Config.StorageBucket, storagePath);

        return publicUrl;
    }

    public async Task<bool> DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Config.Url))
            return false;

        var storagePath = ExtractStoragePath(relativePath);
        if (string.IsNullOrEmpty(storagePath))
            return false;

        try
        {
            await Client.InitializeAsync();
            var bucket = Client.Storage.From(Config.StorageBucket);
            await bucket.Remove(new List<string> { storagePath });
            logger.LogInformation("Deleted file at {StoragePath} from Supabase", storagePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete file at {StoragePath}", storagePath);
            return false;
        }
    }

    public string GetAbsoluteUrl(string relativePath)
    {
        return ExtractStoragePath(relativePath) is { } path
            ? GetPublicUrl(path)
            : relativePath;
    }

    private string GetPublicUrl(string storagePath)
    {
        var baseUrl = Config.Url.TrimEnd('/');
        return $"{baseUrl}/storage/v1/object/public/{Config.StorageBucket}/{storagePath}";
    }

    private string ExtractStoragePath(string urlOrPath)
    {
        if (urlOrPath.Contains("/storage/v1/object"))
        {
            var idx = urlOrPath.IndexOf($"/{Config.StorageBucket}", StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? urlOrPath[(idx + 1)..] : urlOrPath;
        }
        return urlOrPath.TrimStart('/');
    }
}
