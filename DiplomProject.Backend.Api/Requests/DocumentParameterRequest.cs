using System.ComponentModel.DataAnnotations;

namespace DiplomProject.Backend.Api.Requests
{
    public class DocumentParameterRequest
    {
        [Required]
        public string Name { get; set; }

        public string Category { get; set; }

        [Required]
        public string Extension { get; set; }

        [Required]
        public int AuthorId { get; set; }
    }
}
