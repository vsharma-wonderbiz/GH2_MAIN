using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class MappingTable
    {
        public int MappingId { get; private set; }

        public int AssetId { get; private set; }
        public int TagId { get; private set; }
        public Assets Asset { get; private set; }
        public Tag Tag { get; private set; }
        public string OpcNodeId { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public ICollection<SensorRawData> SensorData { get; private set; } = new List<SensorRawData>();
        public ICollection<NodeLastData> NodeLastData { get; private set; } = new List<NodeLastData>();
        public ICollection<TransactionData> TransactionData { get; private set; } = new List<TransactionData>();

        public ICollection<ProtocolConfig> ModbusConifg { get; private set; } = new List<ProtocolConfig>();


        private MappingTable() { }

        public MappingTable(int assetId, int tagId, string opcNodeId)
        {
            if (assetId <= 0)
                throw new ArgumentException("AssetId must be greater than 0", nameof(assetId));

            if (tagId <= 0)
                throw new ArgumentException("TagId must be greater than 0", nameof(tagId));

            if (string.IsNullOrWhiteSpace(opcNodeId))
                throw new ArgumentException("OpcNodeId is required", nameof(opcNodeId));

            AssetId = assetId;
            TagId = tagId;
            OpcNodeId = opcNodeId;
        }
    }
}