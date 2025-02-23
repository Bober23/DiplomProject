using Amazon.S3;
using Amazon.S3.Model;
using DiplomProject.Backend.ImageProcessingService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DiplomProject.Backend.ImageProcessingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IImageProcessor _imageProcessor;
        public ImageController(IImageProcessor imageProcessor)
        {
            _imageProcessor = imageProcessor;
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            // Сохранение в S3
            var key = $"{Guid.NewGuid()}_{file.FileName}";
            // Обработка изображения (например, сжатие)
            await _imageProcessor.LoadToS3Cloud(memoryStream, key, file.ContentType);
            return Ok(key);
        }
    }
}
