using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class StackKpiResponse
    {
        public required string KpiName { get; set; }
        public int NoOfStacks { get; set; }
        public int NoOfWeeks { get; set; }

        public required List<FilteredData> values { get;set; }
    }

    public class FilteredData
    {
        public DateTime StartTime { get; set; }
        public DateTime Endpoint { get; set; }
        public required string Assetname { get; set; }
        public int WeekNumber { get; set; }
        public float value { get; set; }
    }
}
