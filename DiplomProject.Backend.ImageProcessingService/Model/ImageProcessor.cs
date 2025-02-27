
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
using SixLabors.ImageSharp.PixelFormats;

namespace DiplomProject.Backend.ImageProcessingService.Model
{
    public class ImageProcessor : IImageProcessor
    {
        private readonly IAmazonS3 _s3Client;
        private const string BucketName = "shishka";
        private const int _compressionQuality = 70;
        private const int _blockSize = 15;
        private const int _binConstant = 5;

        public ImageProcessor(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task<MemoryStream> BinarizeFile(MemoryStream fileStream)
        {
            using var image = await Image.LoadAsync<Rgba32>(fileStream);
            var bytes = ConvertImageToGrayscaleArray(image);
            var binarizedBytes = AdaptiveBinarization(bytes, image.Width, image.Height, _blockSize, _binConstant);
            using var binImage = Image.LoadPixelData<L8>(binarizedBytes, image.Width, image.Height);
            var outputStream = new MemoryStream();
            binImage.SaveAsPng(outputStream);
            outputStream.Position = 0;
            return outputStream;
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
            await image.SaveAsync(outputStream, encoder);

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
            };

            // Для дебага можно добавить логирование
            // AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;

            await _s3Client.PutObjectAsync(putRequest);
            return fileUri;
        }

        private byte[] AdaptiveBinarization(byte[] image, int width, int height, int blockSize, int constant)
        {
            byte[] binaryImage = new byte[image.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sum = 0;
                    int count = 0;

                    // Вычисление среднего значения в блоке
                    for (int dy = -blockSize / 2; dy <= blockSize / 2; dy++)
                    {
                        for (int dx = -blockSize / 2; dx <= blockSize / 2; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                int index = ny * width + nx;
                                sum += image[index];
                                count++;
                            }
                        }
                    }

                    int threshold = (sum / count) - constant;
                    int currentIndex = y * width + x;
                    binaryImage[currentIndex] = image[currentIndex] < threshold ? (byte)0 : (byte)255;
                }
            }

            return binaryImage;
        }

        private byte[] ConvertImageToGrayscaleArray(Image<Rgba32> image)
        {
            // Загружаем изображение из MemoryStream
            

            // Преобразуем в градации серого
            image.Mutate(x => x.Grayscale());

            // Создаем массив для результата
            byte[] grayscaleData = new byte[image.Width * image.Height];

            // Получаем доступ к пикселям
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    // Получаем доступ к строке пикселей
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    // Обрабатываем каждый пиксель в строке
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // В градациях серого все каналы (R, G, B) имеют одинаковое значение
                        grayscaleData[y * image.Width + x] = pixelRow[x].R;
                    }
                }
            });

            return grayscaleData;
        }

    }
}
