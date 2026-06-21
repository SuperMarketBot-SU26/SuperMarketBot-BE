namespace SmartMarketBot.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string subFolder,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    string GetAbsoluteUrl(string relativePath);
}
