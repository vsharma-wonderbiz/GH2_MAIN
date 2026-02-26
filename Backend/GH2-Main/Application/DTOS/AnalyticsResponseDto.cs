using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class AnalyticsResponseDto
    {
        public required string AsseName { get; set; }

        public required string TagName { get; set; }

        public required List<ValueDto> Values { get; set; }
         public required int count { get; set; }
    }

    public class ValueDto
    {
        public DateTime TimeStamp { get; set; }
        public float Value { get; set; }
    }
}
