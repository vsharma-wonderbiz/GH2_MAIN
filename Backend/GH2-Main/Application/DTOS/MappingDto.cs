using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class MappingDto
    {
        public int MappingId { get; set; }
        public required string AssetName { get; set; }
        public required string TagName { get; set; }
    }
}
