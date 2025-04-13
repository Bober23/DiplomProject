using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DiplomProject.DTOLibrary
{
    public class DocFile
    {
        public int Id {  get; set; }

        public string Name { get; set; }

        public DateTime LoadDate { get; set; }

        public string LinkToFile { get; set; }

        [JsonIgnore]
        public virtual Document Document { get; set; }
    }
}
