using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class KpiMappingResultDto
    {
        public string KpiName { get; set; }
        public List<AssetMappingDto> Assets { get; set; } = new();
    }

    public class AssetMappingDto
    {
        public string AssetName { get; set; }
        public float? KpiValue { get; set; }
        public List<TagMappingDto> Mappings { get; set; } = new();
    }

    public class TagMappingDto
    {
        public int MappingId { get; set; }
        public int TagId { get; set; }

        public string Tagname { get; set; }
        public float AvgValue { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
    }
}
