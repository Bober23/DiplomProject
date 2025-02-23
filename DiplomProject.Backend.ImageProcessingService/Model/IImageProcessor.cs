
namespace DiplomProject.Backend.ImageProcessingService.Model
{
    public interface IImageProcessor
    {
        public Task<string> LoadToS3Cloud(MemoryStream fileStream, string fileUri, string contentType);
        public Task<MemoryStream> GetFromS3Cloud(string filekey);
        public Task<MemoryStream> CompressFile(MemoryStream fileStream);
        public Task<MemoryStream> BinarizeFile(MemoryStream fileStream);
    }
}
