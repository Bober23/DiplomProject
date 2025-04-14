using Amazon.S3;
using Amazon.S3.Model;
using DiplomProject.Backend.ImageProcessingService.Model;
using DiplomProject.DTOLibrary;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;

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

        [EnableCors("AllowAll")]
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

        [EnableCors("AllowAll")]
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

        [EnableCors("AllowAll")]
        [HttpPost("split/")]
        public async Task<IActionResult> SplitImagesBySelection()
        {
            try
            {
                if (!Request.HasFormContentType)
                    return BadRequest("Expected multipart/form-data");

                var form = await Request.ReadFormAsync();

                // Получаем изображение
                var imageFile = form.Files["image"];
                if (imageFile == null || imageFile.Length == 0)
                    return BadRequest("Image file is required");

                // Получаем селекты
                var selectionsJson = form["selections"];
                var selections = selectionsJson.Count > 0
                    ? JsonConvert.DeserializeObject<List<ImageSelection>>(selectionsJson[0])
                    : null;

                using var memoryStream = new MemoryStream();
                await imageFile.CopyToAsync(memoryStream);
                var result = _imageProcessor.SplitImage(memoryStream, selections);
                // Конвертация результатов
                var response = new ProcessingResultResponse
                {
                    Segments = ConvertImagesToByteArrays(result.SplitSegments),
                    CroppedRegions = ConvertImagesToByteArrays(result.CroppedRegions)
                };

                // Очистка ресурсов
                if (result.ModifiedImage != null)
                {
                    result.ModifiedImage.Dispose();
                }
                if (result.CroppedRegions != null)
                {
                    result.CroppedRegions.ForEach(i => i.Dispose());
                }
                if (result.SplitSegments != null)
                {
                    result.SplitSegments.ForEach(i => i.Dispose());
                }
                
                

                return Ok(response);
            }
            catch (JsonException ex)
            {
                return BadRequest($"Invalid selections format: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private List<byte[]> ConvertImagesToByteArrays(List<Image<Rgba32>> images)
        {
            var result = new List<byte[]>();
            var encoder = new PngEncoder();

            foreach (var image in images)
            {
                using var ms = new MemoryStream();
                image.Save(ms, encoder);
                result.Add(ms.ToArray());
            }

            return result;
        }
    }
}
