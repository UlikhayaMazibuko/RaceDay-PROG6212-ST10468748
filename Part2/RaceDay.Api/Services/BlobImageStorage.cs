using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services
{
    // PART 3 ADDITION: This interface is used so the controller doesn't depend on Azure directly
    public interface IImageStorage
    {
        Task<string> UploadAsync(IFormFile file, string folder);
    }

    // PART 3 ADDITION: This class uploads images to Azure Blob Storage.
    // The container is created the first time it's used and is public so the website can show the images
    public class BlobImageStorage : IImageStorage
    {
        private readonly string? _connectionString;
        private readonly string _containerName;
        private BlobContainerClient? _container;

        public BlobImageStorage(IConfiguration configuration)
        {
            _connectionString = configuration["AzureStorage:ConnectionString"];
            _containerName = configuration["AzureStorage:ContainerName"] ?? "raceday-images";
        }

        public async Task<string> UploadAsync(IFormFile file, string folder)
        {
            var container = await GetContainerAsync();

            // Uses a random file name so two images with the same name don't replace each other
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blob.Uri.ToString();
        }

        private async Task<BlobContainerClient> GetContainerAsync()
        {
            if (_container is not null)
            {
                return _container;
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage is not configured. Add AzureStorage:ConnectionString to the API's User Secrets.");
            }

            var container = new BlobContainerClient(_connectionString, _containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            _container = container;
            return container;
        }
    }
}
