using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomProject.DTOLibrary
{
    public class DocMarkup
    {
        public object Content { get; set; }
        public MarkupType Type { get; set; }
    }

    public enum MarkupType
    {
        Text,
        Image,
    }
}
