using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

public class S3Service
{
    private readonly IAmazonS3 _s3Client;
    private const string BucketName = "shishka";

    public S3Service()
    {
        AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;

        var config = new AmazonS3Config
        {
            ServiceURL = "https://hb.ru-msk.vkcloud-storage.ru", // Кастомный эндпоинт
            ForcePathStyle = true, // Обязательно!
        };

        // Явное указание учетных данных
        var credentials = new BasicAWSCredentials("i7tLZg7cKz9SVRwTEKRVia", "a27QiqQq64j93yKnTbeaeHqWMcCmxJ3WrUTvgLMfy9KZ");
        _s3Client = new AmazonS3Client(credentials, config);
    }

    public async Task UploadFileAsync(string key, Stream fileStream)
    {
        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = fileStream
        };

        await _s3Client.PutObjectAsync(request);
        Console.WriteLine("Файл успешно загружен!");
    }
}
