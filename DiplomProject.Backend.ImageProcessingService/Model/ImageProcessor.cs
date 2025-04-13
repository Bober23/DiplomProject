
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
using DiplomProject.DTOLibrary;

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
            Console.WriteLine("TEST");
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

        public ImageProcessorResult SplitImage(MemoryStream imageStream, List<ImageSelection> selections)
        {
            imageStream.Position = 0;
            var result = new ImageProcessorResult();

            // Загрузка и модификация изображения
            Image<Rgba32> originalImage = Image.Load<Rgba32>(imageStream);
            Image<Rgba32> modifiedImage = originalImage.Clone();
            result.CroppedRegions = new List<Image<Rgba32>>();

            // Список для координат разделения
            var splitYCoordinates = new List<int>();
            if (selections == null || selections.Count == 0)
            {
                result.SplitSegments = new List<Image<Rgba32>>();
                result.SplitSegments.Add(originalImage);
                return result;
            }
            foreach (var selection in selections)
            {
                List<Point> points = new List<Point>();
                foreach (var point in selection.Points)
                {
                    points.Add(new Point(x: (int)point.X, y: (int)point.Y));
                }
                // Преобразуем точки выделения в прямоугольник
                var (x, y, width, height) = ConvertPointsToRectangle(points);
                if (width <= 0 || height <= 0) continue;

                // Сохраняем Y-координату для разделения
                splitYCoordinates.Add(y + height); // Добавляем нижнюю границу (максимальный Y)

                // Вырезаем регион
                var cropped = originalImage.Clone(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
                result.CroppedRegions.Add(cropped);

                // Закрашиваем белым
                modifiedImage.Mutate(ctx => ctx
                    .Fill(Color.White, new Rectangle(x, y, width, height)));
            }

            // Добавляем границы изображения
            splitYCoordinates.Add(0);
            splitYCoordinates.Add(modifiedImage.Height);

            // Обработка координат разделения
            result.SplitSegments = SplitImageByYCoordinates(modifiedImage, splitYCoordinates);
            result.ModifiedImage = modifiedImage;

            return result;
        }

        private List<Image<Rgba32>> SplitImageByYCoordinates(Image<Rgba32> image, List<int> yCoordinates)
        {
            // Убираем дубликаты и сортируем
            var uniqueSortedY = yCoordinates
                .Distinct()
                .OrderBy(y => y)
                .Where(y => y >= 0 && y <= image.Height)
                .ToList();

            var segments = new List<Image<Rgba32>>();
            int previousY = 0;

            foreach (var currentY in uniqueSortedY)
            {
                if (currentY <= previousY) continue;

                int segmentHeight = currentY - previousY;

                if (segmentHeight > 0)
                {
                    var segment = image.Clone(ctx => ctx
                        .Crop(new Rectangle(0, previousY, image.Width, segmentHeight)));

                    segments.Add(segment);
                }

                previousY = currentY;
            }

            return segments;
        }

        private (int x, int y, int width, int height)
            ConvertPointsToRectangle(List<Point> points)
        {
            if (points.Count != 4)
                throw new ArgumentException("Selection must contain exactly 4 points");

            // Находим границы прямоугольника
            int x1 = points.Min(p => p.X);
            int y1 = points.Min(p => p.Y);
            int x2 = points.Max(p => p.X);
            int y2 = points.Max(p => p.Y);

            return (
                x: x1,
                y: y1,
                width: x2 - x1,
                height: y2 - y1
            );
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

    public class ImageProcessorResult
    {
        public Image<Rgba32> ModifiedImage { get; set; }
        public List<Image<Rgba32>> CroppedRegions { get; set; }
        public List<Image<Rgba32>> SplitSegments { get; set; }
    }
}
