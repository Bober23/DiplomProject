using Amazon.S3;
using Amazon.S3.Model;
using DiplomProject.Backend.ImageProcessingService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DiplomProject.Backend.ImageProcessingService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IImageProcessor _imageProcessor;
        public ImageController(IImageProcessor imageProcessor)
        {
            _imageProcessor = imageProcessor;
        }
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string fileDirectory)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var key = $"{fileDirectory}/{file.FileName}";
            using var compressedStream = await _imageProcessor.CompressFile(memoryStream);
            await _imageProcessor.LoadToS3Cloud(compressedStream, key, file.ContentType);
            return Ok(key);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadImage([FromQuery] string linkToFile, [FromQuery] bool binarized)
        {
            if (linkToFile == null || linkToFile == string.Empty)
                return BadRequest("Empty link");
            // Обработка изображения (например, сжатие)
            var stream = await _imageProcessor.GetFromS3Cloud(linkToFile);
            if (stream == null || stream.Length == 0)
            {
                return BadRequest("Download error");
            }
            if (binarized)
            {
                var binarizedStream = await _imageProcessor.BinarizeFile(stream);
                return File(binarizedStream, $"image/{linkToFile.Substring(linkToFile.LastIndexOf('.') + 1)}");
            }
            return File(stream, $"image/{linkToFile.Substring(linkToFile.LastIndexOf('.') + 1)}");
        }

        
    }
}
