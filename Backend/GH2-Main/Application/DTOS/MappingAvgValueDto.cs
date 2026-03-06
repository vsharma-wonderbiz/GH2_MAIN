using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
  public class MappingAvgValueDto
    {
            public int MappingId { get; set; }
            public float AvgValue { get; set; }
            public float MinValue { get; set; }
            public float MaxValue { get; set; }
        
    }
}
