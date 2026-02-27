using System;

namespace Domain.Entities
{
    public class WeeklyAggregatedData
    {
        public int Id { get; private set; }

        public int AssetId { get; private set; }
        public int MappingId { get; private set; }
        public DateTime WeekStartDate { get; private set; }
        public DateTime WeekEndDate { get; private set; }
        public float AverageValue { get; private set; }
        public float MinValue { get; private set; }
        public float MaxValue { get; private set; }
        public int TotalSamples { get; private set; }

        public int DaysCount { get; private set; }

        public bool IsFinal { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public MappingTable Mapping { get; private set; }

        public WeeklyAggregatedData() { }

        // Constructor enforces required values
        public WeeklyAggregatedData(int assetid,int mappingId, DateTime weekStart, DateTime weekEnd,
                                     float avgValue, float minValue, float maxValue, int totalSamples, int daysCount, bool isFinal)
        {
            if (weekEnd < weekStart)
                throw new ArgumentException("WeekEndDate cannot be earlier than WeekStartDate.");

            if (totalSamples < 0)
                throw new ArgumentOutOfRangeException(nameof(totalSamples), "TotalSamples cannot be negative.");

            if (minValue > maxValue)
                throw new ArgumentException("MinValue cannot be greater than MaxValue.");

            AssetId = assetid;
            MappingId = mappingId;
            WeekStartDate = weekStart;
            WeekEndDate = weekEnd;
            AverageValue = avgValue;
            MinValue = minValue;
            MaxValue = maxValue;
            TotalSamples = totalSamples;
            DaysCount = daysCount;
            IsFinal = isFinal;
        }

        public void UpdateAggregates(float avgValue, float minValue, float maxValue, int totalSamples,DateTime WeeEndData)
        {
            if (totalSamples <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalSamples), "TotalSamples must be positive.");

            if (minValue > maxValue)
                throw new ArgumentException("MinValue cannot be greater than MaxValue.");

            AverageValue = avgValue;
            MinValue = minValue;
            MaxValue = maxValue;
            TotalSamples = totalSamples;
            WeekEndDate = WeeEndData;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateIsFinale(bool change)
        {
            IsFinal = change;
        }
    }
}