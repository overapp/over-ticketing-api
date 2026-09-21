namespace Application.Abstractions.Storage;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream contentStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
