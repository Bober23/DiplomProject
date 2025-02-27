using Amazon.S3;
using Amazon.S3.Model;

namespace DiplomProject.Backend.DocumentProcessingService
{
    public class S3Loader
    {
        private readonly IAmazonS3 _client;
        private readonly string BucketName = "shishka";
        public S3Loader(IAmazonS3 client)
        {
            _client = client;
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
            };

            // Для дебага можно добавить логирование
            // AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;

            await _client.PutObjectAsync(putRequest);
            return fileUri;
        }

        public async Task<MemoryStream> GetFromS3Cloud(string fileLink)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = fileLink,
            };

            using var response = await _client.GetObjectAsync(getRequest);

            using var responseStream = response.ResponseStream;
            var memoryStream = new MemoryStream();
            await responseStream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
    }
}
