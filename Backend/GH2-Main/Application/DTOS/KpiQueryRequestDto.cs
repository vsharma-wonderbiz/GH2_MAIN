// Application/DTOS/KpiQueryRequestDto.cs
namespace Application.DTOS
{
    public class KpiQueryRequestDto
    {
        public int TagId { get; set; }
        public KpiTimeRange TimeRange { get; set; }

        // Only used when TimeRange == Custom
        public DateTime? CustomStart { get; set; }
        public DateTime? CustomEnd { get; set; }
    }

    public enum KpiTimeRange
    {
        LastHour,
        Last24Hours,
        LastWeek,   // served from cache
        Custom      // falls back to live calc if not in cache
    }
}