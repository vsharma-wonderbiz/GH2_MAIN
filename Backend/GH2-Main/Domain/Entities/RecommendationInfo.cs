using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class RecommendationInfo
    {
        public int Id { get; private set; }
        public int MappingId { get; private set; }

        public string AssetName { get; private set; }

        public string SignalName { get; private set; }

        public string RecommendationType { get; private set; }

        public float CurrentVal { get; private set; }

        public float TriggerVal { get; private set; }

        public string Message { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? ResolvedAt { get; private set; }

        public MappingTable? Mapping { get; private set; }

        public string Status { get; private set; }



        public RecommendationInfo(int mappingId, string assetName, string signalName, string recommendationType, float currentVal, float triggerVal, string message)
        {
            MappingId=mappingId;
            AssetName = assetName ?? throw new ArgumentNullException(nameof(assetName));
            SignalName=signalName ?? throw new ArgumentNullException(nameof(signalName));
            RecommendationType= recommendationType ?? throw new ArgumentNullException(nameof(recommendationType));
            CurrentVal = currentVal;
            TriggerVal = triggerVal;
            Message=message;
            CreatedAt = DateTime.UtcNow;
            Status = "Active";
        }

        public void Resolve()
        {
            if (Status == "Resolved") return;
            Status = "Resolved";
            ResolvedAt = DateTime.UtcNow;
        }


    }

}
