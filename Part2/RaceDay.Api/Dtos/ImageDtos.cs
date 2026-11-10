using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    // PART 3 ADDITION: The form used to upload an image, it has one field called File
    public class ImageUploadRequest
    {
        /// <summary>The image file: JPG, PNG, WEBP or GIF, up to 5 MB.</summary>
        [Required]
        public IFormFile? File { get; set; }
    }

    // PART 3 ADDITION: Sent back after the upload with the Azure link to the image
    public class ImageUploadResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
