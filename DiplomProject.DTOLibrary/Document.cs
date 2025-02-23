

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DiplomProject.DTOLibrary
{
    public class Document
    {
        [Key]
        public int id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Category { get; set; }

        [Required]
        public string Extension { get; set; }


        public string? ContentLink { get; set; }

        public string? ContentImagesLink { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
