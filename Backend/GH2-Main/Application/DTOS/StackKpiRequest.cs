using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class StackKpiRequest
    {
        public int KpiId { get; set; }

        public string KpiName { get; set; }

        public int NoOfStack { get; set; }

        public int NoOfWeeks  { get; set;}
         
    }
}
