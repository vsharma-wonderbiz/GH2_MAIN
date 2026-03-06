using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class KpiTable
    {
        public int Id { get; private set; }
        public string KpiName { get; private set; }
        public string AssetName { get; private set; }
        public string Level { get; private set; }        // Plant or Stack
        public float KpiValue { get; private set; }

        public int WeekNumber { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public DateTime CalculatedAt { get; private set; } = DateTime.UtcNow;

        public KpiTable() { }

        public KpiTable(string kpiName, string assetName, string level,
                 float kpiValue, int weekNumber, DateTime startTime, DateTime endTime)
        {
            if (string.IsNullOrWhiteSpace(kpiName))
                throw new ArgumentException("KpiName cannot be null or empty");

            if (string.IsNullOrWhiteSpace(assetName))
                throw new ArgumentException("AssetName cannot be null or empty");

            if (string.IsNullOrWhiteSpace(level))
                throw new ArgumentException("Level cannot be null or empty");

            if (kpiValue < 0)
                throw new ArgumentException("KpiValue cannot be negative");

            if (startTime >= endTime)
                throw new ArgumentException("StartTime must be earlier than EndTime");

            KpiName = kpiName;
            AssetName = assetName;
            Level = level;
            KpiValue = kpiValue;
            WeekNumber = weekNumber;
            StartTime = startTime;
            EndTime = endTime;
        }

        public void IncrementWeek()
        {
            WeekNumber++;
        }
    }
}
