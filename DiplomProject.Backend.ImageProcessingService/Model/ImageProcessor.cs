
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace DiplomProject.Backend.ImageProcessingService.Model
{
    public class ImageProcessor : IImageProcessor
    {
        private readonly IAmazonS3 _s3Client;
        private const string BucketName = "shishka";

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

        public Task<MemoryStream> CompressFile(MemoryStream fileStream)
        {
            throw new NotImplementedException();
        }

        public Task<MemoryStream> GetFromS3Cloud(string filekey)
        {
            throw new NotImplementedException();
        }

        public async Task<string> LoadToS3Cloud(MemoryStream fileStream, string fileUri, string contentType)
        {
            // Обработка изображения (например, сжатие)

            // Сохранение в S3
            var putRequest = new PutObjectRequest
            {
                
                BucketName = BucketName,
                Key = fileUri,
                InputStream = fileStream,
                ContentType = contentType
            };
            putRequest.Headers["x-amz-content-sha256"] = "UNSIGNED-PAYLOAD";
            await _s3Client.PutObjectAsync(putRequest);
            return "cool";
        }
    }
}
