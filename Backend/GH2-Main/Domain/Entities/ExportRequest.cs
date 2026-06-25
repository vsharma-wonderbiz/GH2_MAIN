using System;

namespace Domain.Entities
{
    public class ExportRequest
    {
        public int ExportRequestId { get; private set; }
        public string AssetName { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public string Status { get; private set; } = "Pending";   
        public string RequestedBy { get; private set; }
        public string? FilePath { get; private set; }
        public DateTime RequestedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        public ICollection<ExportRequestTags> RequestedTags { get; private set; }

        // Parameterless constructor for EF Core
        public ExportRequest() { }

        public ExportRequest( string assetName, DateTime startTime, DateTime endTime, string requestedBy)
        {
         

            if (string.IsNullOrWhiteSpace(assetName))
                throw new ArgumentException("AssetName cannot be null or empty");

            if (startTime >= endTime)
                throw new ArgumentException("StartTime must be earlier than EndTime");

            if (string.IsNullOrWhiteSpace(requestedBy))
                throw new ArgumentException("RequestedBy cannot be null or empty");

            AssetName = assetName;
            StartTime = startTime;
            EndTime = endTime;
            RequestedBy = requestedBy;
            RequestedAt = DateTime.UtcNow;
       
        }

        public void MarkAsCompleted()
        {
            Status = "Completed";
        }

        public void MarkAsFailed()
        {
            Status = "Failed";
        }

        public void ResetStatus()
        {
            Status = "Pending";
        }

        public void MarkAsCompletedWithFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("FilePath cannot be empty");

            FilePath = filePath;
            Status = "Completed";
            CompletedAt = DateTime.UtcNow;
        }
    }
}
