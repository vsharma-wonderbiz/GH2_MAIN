using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class WeeklyAvgResposeFromDb
    {
        public float Average { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float Sum { get; set; }
        public int Count { get; set; }

        public bool IsFinal { get; set; }
    }
}
