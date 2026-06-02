using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    //these is basically to get all the letaest week kpi value baseond on name 
    public class KpiDto
    {
        public required string KpiName { get; set; }
        public float KpiValue { get; set; }
        public required string Level { get; set; }
    }

    public class KpiResponseDto
    {
        public required string StackName { get; set; }
        public int Week { get; set; }
        public required List<KpiDto> Data { get; set; }
    }
}
