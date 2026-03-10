using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class OpcConfigDto
    {
        public string AssetName { get; set; }

        public string TagName { get; set; }

        public string OpcNodeId { get; set; }

        public int SlaveId { get; set; }

        public int RegisterAddress { get; set; }

        public string Datatype { get; set; }

        public int RegisterCount { get; set; }

        public int FunctionCode { get; set; }

        public string Unit { get; set; }

        public string DisplayName { get; set; }

        public double Deadband { get; set; }
    }
}
