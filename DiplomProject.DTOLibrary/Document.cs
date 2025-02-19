

using System.ComponentModel.DataAnnotations;

namespace DiplomProject.DTOLibrary
{
    public class Document
    {
        [Key]
        public int id { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public string Extension { get; set; }

        public string ContentLink { get; set; }

        public string ContentImagesLink { get; set; }

        public virtual User User { get; set; }
    }
}
