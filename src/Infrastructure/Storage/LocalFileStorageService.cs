using Application.Abstractions.Storage;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Storage;

internal sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;

    public LocalFileStorageService(IHostEnvironment environment)
    {
        _storageRoot = Path.Combine(environment.ContentRootPath, "App_Data", "Attachments");
        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
        }
    }

    public async Task<string> UploadAsync(
        Stream contentStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        string extension = Path.GetExtension(fileName);
        string uniqueName = $"{Guid.NewGuid():N}{extension}";
        string fullPath = Path.Combine(_storageRoot, uniqueName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await contentStream.CopyToAsync(fileStream, cancellationToken);

        return uniqueName;
    }

    public Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(_storageRoot, storagePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(_storageRoot, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
