using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Entities
{
    public class MappingTable
    {
        public int MappingId { get; private set; }

        // Foreign keys
        public int AssetId { get; private set; }
        public int TagId { get; private set; }

        // Navigation properties
        public Assets Asset { get; private set; }
        public Tag Tag { get; private set; }

        public string OpcNodeId { get; private set; }
        public int RegisterAddress { get; private set; }
        public string DataType { get; private set; }
        public int RegisterCount { get; private set; }
        public int Deadband { get; private set; }
        public int FunctionCode { get; private set; }
        public int SlaveId { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.Now;

        public ICollection<SensorRawData> SensorData =new List<SensorRawData>();

        public ICollection<NodeLastData> NodeLastData = new List<NodeLastData>();

        public ICollection<TransactionData> TrnasactionData = new List<TransactionData>();



        // EF Core needs a parameterless constructor
        private MappingTable() { }

        // Business constructor
        public MappingTable(
            int assetId,
            int tagId,
            string opcNodeId,
            int registerAddress,
            string dataType,
            int registerCount,
            int deadband,
            int functionCode,
            int slaveId)
        {
            if (assetId <= 0)
                throw new ArgumentException("AssetId must be greater than 0", nameof(assetId));

            if (tagId <= 0)
                throw new ArgumentException("TagId must be greater than 0", nameof(tagId));

            if (string.IsNullOrWhiteSpace(opcNodeId))
                throw new ArgumentException("OpcNodeId is required", nameof(opcNodeId));

            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("DataType is required", nameof(dataType));

            if (registerCount <= 0)
                throw new ArgumentException("RegisterCount must be greater than 0", nameof(registerCount));

            if (functionCode <= 0)
                throw new ArgumentException("FunctionCode must be greater than 0", nameof(functionCode));

            if (slaveId <= 0)
                throw new ArgumentException("SlaveId must be greater than 0", nameof(slaveId));

            AssetId = assetId;
            TagId = tagId;
            OpcNodeId = opcNodeId;
            RegisterAddress = registerAddress;
            DataType = dataType;
            RegisterCount = registerCount;
            Deadband = deadband;
            FunctionCode = functionCode;
            SlaveId = slaveId;
        }
    }
}