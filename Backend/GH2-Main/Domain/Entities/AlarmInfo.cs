using System;
using System.Data;

namespace Domain.Entities
{
    public class AlarmInfo
    {
        public int Id { get; private set; }
        public int MappingId { get; private set; }
        public string SignalName { get; private set; }
        public float Value { get; private set; }
        public string AlarmType { get; private set; }
        public string Status { get; private set; } 
        public DateTime CreatedAt { get; private set; }
        public DateTime? ResolvedAt { get; private set; }

        public MappingTable Mapping { get; private set; }

        public AlarmInfo(int mappingId, string signalName, float value, string alarmType)
        {
            MappingId = mappingId;
            SignalName = signalName ?? throw new ArgumentNullException(nameof(signalName));
            Value = value;
            AlarmType = alarmType ?? throw new ArgumentNullException(nameof(alarmType));
            Status = "Active";
            CreatedAt = DateTime.UtcNow;
        }
        public void Resolve()
        {
            if (Status == "Resolved") return;
            Status = "Resolved";
            ResolvedAt = DateTime.UtcNow;
        }
        
        public void UpdateValue(float newValue)
        {
            Value = newValue;
        }

        public void ChangeType(string newType)
        {
            AlarmType = newType ?? throw new ArgumentNullException(nameof(newType));
        }
    }
}