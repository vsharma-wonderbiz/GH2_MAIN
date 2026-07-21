using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class EfficiencyResult
    {
        public required string StackName { get; set; }
        public double LatestEfficiency { get; set; }
        public double OperationalHours { get; set; }
        public required string Status { get; set; }
    }
}
