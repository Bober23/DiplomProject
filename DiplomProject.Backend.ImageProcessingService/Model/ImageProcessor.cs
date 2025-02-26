
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats;

namespace DiplomProject.Backend.ImageProcessingService.Model
{
    public class ImageProcessor : IImageProcessor
    {
        private readonly IAmazonS3 _s3Client;
        private const string BucketName = "shishka";
        private const int _compressionQuality = 70;

        public ImageProcessor(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
            AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
            AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.Always;
            AWSConfigs.LoggingConfig.LogMetrics = true;
        }

        public Task<MemoryStream> BinarizeFile(MemoryStream fileStream)
        {
            throw new NotImplementedException();
        }

        public async Task<MemoryStream> CompressFile(MemoryStream fileStream)
        {
            // Асинхронная загрузка изображения
            fileStream.Position = 0;
            var imageFormat = Image.DetectFormat(fileStream);
            fileStream.Position = 0;
            using var image = await Image.LoadAsync(fileStream);
            // Обработка изображения
            IImageEncoder encoder = imageFormat.Name switch
            {
                "JPEG" => new JpegEncoder { Quality = _compressionQuality },
                "PNG" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression },
                "WEBP" => new WebpEncoder { Quality = _compressionQuality },
                _ => throw new NotSupportedException($"Format {imageFormat.Name} is not supported")
            };

            // Асинхронное сохранение в MemoryStream
            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, new JpegEncoder { Quality = 90 });

            outputStream.Position = 0;
            return outputStream;
        }

        public async Task<MemoryStream> GetFromS3Cloud(string fileLink)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = fileLink,
            };

            using var response = await _s3Client.GetObjectAsync(getRequest);

            using var responseStream = response.ResponseStream;
            var memoryStream = new MemoryStream();
            await responseStream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        public async Task<string> LoadToS3Cloud(MemoryStream fileStream, string fileUri, string contentType)
        {
            // Сбросьте позицию потока перед чтением
            fileStream.Position = 0;

            var putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = fileUri,
                InputStream = fileStream,
                ContentType = contentType,
                Headers = { ["x-amz-content-sha256"] = "UNSIGNED-PAYLOAD" }
            };

            // Для дебага можно добавить логирование
            // AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;

            await _s3Client.PutObjectAsync(putRequest);
            return fileUri;
        }
    }
}
