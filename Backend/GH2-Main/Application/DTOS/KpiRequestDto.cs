using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class KpiRequestDto
    {
        public int tagId { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
    }
}
