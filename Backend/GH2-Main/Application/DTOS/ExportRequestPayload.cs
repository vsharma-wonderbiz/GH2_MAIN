using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class ExportRequestPayload
    {
        public int ExportJobID { get; set; }
        public string AssetName { get; set; }
        public List<string> TagNames { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
