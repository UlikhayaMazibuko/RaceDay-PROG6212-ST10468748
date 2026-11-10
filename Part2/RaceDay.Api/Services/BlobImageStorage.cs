using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services
{
    // Anything that can store an uploaded image and give back a public URL for it.
    public interface IImageStorage
    {
        Task<string> UploadAsync(IFormFile file, string folder);
    }

    // Stores images in an Azure Blob Storage container. The container is created on first use
    // with public read access on blobs, so the MVC site can show images straight from the URL.
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

            // A random name stops two uploads with the same file name overwriting each other.
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
