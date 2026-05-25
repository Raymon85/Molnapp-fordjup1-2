using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RecruitmentPortal.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration config, ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = config["BlobStorage:ContainerName"] ?? "cv-uploads";
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

        _logger.LogInformation("Uploaded blob {BlobName} to container {Container}", blobName, _containerName);
        return blobClient.Uri.ToString();
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.GetPropertiesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob storage health check failed");
            return false;
        }
    }
}
