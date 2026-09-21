using Application.Abstractions.Storage;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

internal sealed class AzureBlobStorageService : IFileStorageService
{
    private readonly Lazy<BlobContainerClient> _containerClient;

    public AzureBlobStorageService(IOptions<AzureBlobStorageOptions> options)
    {
        AzureBlobStorageOptions storageOptions = options.Value;
        _containerClient = new Lazy<BlobContainerClient>(() =>
        {
            if (string.IsNullOrWhiteSpace(storageOptions.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is not configured. Please provide a valid connection string in configuration.");
            }

            var serviceClient = new BlobServiceClient(storageOptions.ConnectionString);
            return serviceClient.GetBlobContainerClient(storageOptions.ContainerName);
        });
    }

    private BlobContainerClient ContainerClient => _containerClient.Value;

    public async Task<string> UploadAsync(
        Stream contentStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await ContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        string extension = Path.GetExtension(fileName);
        string uniqueName = $"{Guid.NewGuid():N}{extension}";
        BlobClient blobClient = ContainerClient.GetBlobClient(uniqueName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blobClient.UploadAsync(contentStream, uploadOptions, cancellationToken);

        return uniqueName;
    }

    public async Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = ContainerClient.GetBlobClient(storagePath);

        try
        {
            BlobDownloadStreamingResult response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Content;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound || ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = ContainerClient.GetBlobClient(storagePath);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
