using System;

namespace Domain.Entities
{
    public class ProtocolConfig
    {
        public int Id { get; private set; }
        public int MappingId { get; private set; }
        public int RegisterAddress { get; private set; }
        public int RegisterCount {  get; private set; }
        public int FunctionCode { get; private set; }
        public int SlaveId { get; private set; }

        public MappingTable Mapping { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private ProtocolConfig() { }
        public ProtocolConfig(int mappingId, int registerAddress, int functionCode, int slaveId)
        {
            if (mappingId <= 0)
                throw new ArgumentException("MappingId must be greater than 0", nameof(mappingId));

            if (registerAddress < 0)
                throw new ArgumentException("RegisterAddress cannot be negative", nameof(registerAddress));

            if (functionCode <= 0)
                throw new ArgumentException("FunctionCode must be greater than 0", nameof(functionCode));

            if (slaveId <= 0)
                throw new ArgumentException("SlaveId must be greater than 0", nameof(slaveId));

            MappingId = mappingId;
            RegisterAddress = registerAddress;
            FunctionCode = functionCode;
            SlaveId = slaveId;
        }

        public void UpdateRegisterAddress(int registerAddress)
        {
            if (registerAddress < 0)
                throw new ArgumentException("RegisterAddress cannot be negative", nameof(registerAddress));

            RegisterAddress = registerAddress;
        }

        public void UpdateFunctionCode(int functionCode)
        {
            if (functionCode <= 0)
                throw new ArgumentException("FunctionCode must be greater than 0", nameof(functionCode));

            FunctionCode = functionCode;
        }

        public void UpdateSlaveId(int slaveId)
        {
            if (slaveId <= 0)
                throw new ArgumentException("SlaveId must be greater than 0", nameof(slaveId));

            SlaveId = slaveId;
        }
    }
}