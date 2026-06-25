using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class ExportRequestDto
    {
        public required string AssetNamme {get ;set;} 
        
        public required List<string> TagNames {get;set;}

        public required DateTime StartTime {get;set;}

        public required DateTime EndTime {get;set;}
    }
}
