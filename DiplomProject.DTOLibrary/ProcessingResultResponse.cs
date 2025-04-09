using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomProject.DTOLibrary
{
    public class ProcessingResultResponse
    {
        public List<byte[]> Segments { get; set; }

        public List<byte[]> CroppedRegions { get; set; }
    }
}
