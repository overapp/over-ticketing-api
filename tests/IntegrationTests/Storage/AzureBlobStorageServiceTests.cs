using System.Text;
using Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Storage;

[Collection(nameof(IntegrationTestCollection))]
public sealed class AzureBlobStorageServiceTests(IntegrationTestAppHost appHost)
{
    [Fact]
    public async Task UploadAsync_Should_ThrowInvalidOperationException_WhenConnectionStringIsEmpty()
    {
        // Arrange
        IOptions<AzureBlobStorageOptions> options = Options.Create(new AzureBlobStorageOptions
        {
            ConnectionString = string.Empty,
            ContainerName = "test-container"
        });

        var storageService = new AzureBlobStorageService(options);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            storageService.UploadAsync(stream, "test.txt", "text/plain"));
    }

    [Fact]
    public async Task Upload_Download_AndDelete_Should_WorkEndToEnd_WithAzurite()
    {
        // Arrange
        string? connectionString = await appHost.GetConnectionStringAsync("blobs");
        connectionString.ShouldNotBeNullOrWhiteSpace();

        string containerName = $"test-{Guid.NewGuid():N}";
        IOptions<AzureBlobStorageOptions> options = Options.Create(new AzureBlobStorageOptions
        {
            ConnectionString = connectionString,
            ContainerName = containerName
        });

        var storageService = new AzureBlobStorageService(options);
        byte[] expectedBytes = Encoding.UTF8.GetBytes("Hello Azure Blob Storage!");
        using var uploadStream = new MemoryStream(expectedBytes);

        // Act - Upload
        string storagePath = await storageService.UploadAsync(uploadStream, "document.txt", "text/plain");

        // Assert - Upload
        storagePath.ShouldNotBeNullOrWhiteSpace();
        storagePath.ShouldEndWith(".txt");

        // Act - Download
        Stream? downloadStream = await storageService.DownloadAsync(storagePath);

        // Assert - Download
        downloadStream.ShouldNotBeNull();
        using var memoryStream = new MemoryStream();
        await downloadStream.CopyToAsync(memoryStream);
        byte[] actualBytes = memoryStream.ToArray();
        actualBytes.ShouldBe(expectedBytes);

        // Act - Delete
        await storageService.DeleteAsync(storagePath);

        // Assert - Delete (Download after delete should return null)
        Stream? streamAfterDelete = await storageService.DownloadAsync(storagePath);
        streamAfterDelete.ShouldBeNull();
    }
}
