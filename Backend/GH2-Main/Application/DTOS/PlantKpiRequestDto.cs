using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class PlantKpiRequestDto
    {
         public int KpiId { get; set; }
        public string KpiName { get; set; }

        public int NoOfWeeks { get; set; }
    }
}
