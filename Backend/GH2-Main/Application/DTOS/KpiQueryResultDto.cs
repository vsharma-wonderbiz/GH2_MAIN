// Application/DTOS/KpiQueryResultDto.cs
namespace Application.DTOS
{
    public class KpiQueryResultDto
    {
        public string KpiName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public KpiDataSource Source { get; set; }   // tells the caller: cache or live
        public List<KpiAssetResultDto> Assets { get; set; } = new();
    }

    public class KpiAssetResultDto
    {
        public string AssetName { get; set; }
        public float? KpiValue { get; set; }
        //public List<TagMappingDto> Mappings { get; set; } = new();
    }

    public enum KpiDataSource
    {
        Cache,
        LiveCalculation
    }
}