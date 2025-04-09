using DiplomProject.DTOLibrary;

namespace DiplomProject.Backend.Api.Requests
{
    public class GenerateDocRequest
    {
        public IFormFile Images { get; set; }
        public List<ImageSelection> Selections { get; set; }
    }
}
