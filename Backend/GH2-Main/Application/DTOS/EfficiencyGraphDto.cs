using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class EfficiencyGraphDto
    {
        public List<EfficiencyRecordDto> Actual { get; set; } = new();
        public List<EfficiencyRecordDto> Predicted { get; set; } = new();
    }

    public class EfficiencyRecordDto
    {
        public double OperationalHour { get; set; }
        public double Efficiency { get; set; }
    }
}
