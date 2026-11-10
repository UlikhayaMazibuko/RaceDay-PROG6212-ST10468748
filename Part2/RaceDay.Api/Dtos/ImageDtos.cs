using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    // The form sent when uploading an image (multipart/form-data with one field called File).
    public class ImageUploadRequest
    {
        /// <summary>The image file: JPG, PNG, WEBP or GIF, up to 5 MB.</summary>
        [Required]
        public IFormFile? File { get; set; }
    }

    // Returned after a successful upload: the public Azure Blob Storage URL of the image.
    public class ImageUploadResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
