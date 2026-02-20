using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Tag
    {
        public int TagId { get; private set; }
        public int TagTypeId { get; private set; }
        public string TagName { get; private set; }
        public string Unit { get; private set; }
        public float LowerLimit { get; private set; }
        public float UpperLimit { get; private set; }
        public string DataType { get; private set; }
        public float Deadband { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public TagType TagType { get; private set; }
        public ICollection<MappingTable> Mappings { get; private set; } = new List<MappingTable>();
        private Tag() { }
        public Tag(int tagTypeId, string tagName, string unit, float lowerLimit, float upperLimit, string dataType = null, float deadband = 0)
        {
            if (tagTypeId <= 0)
                throw new ArgumentException("TagTypeId must be greater than 0", nameof(tagTypeId));

            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("TagName is required", nameof(tagName));

            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Unit is required", nameof(unit));

            if (lowerLimit >= upperLimit)
                throw new ArgumentException("LowerLimit must be less than UpperLimit");

            TagTypeId = tagTypeId;
            TagName = tagName;
            Unit = unit;
            LowerLimit = lowerLimit;
            UpperLimit = upperLimit;
            DataType = dataType;
            Deadband = deadband;
        }
        public void UpdateTagName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Tag name cannot be empty", nameof(newName));

            TagName = newName;
        }

        public void UpdateLimits(float lowerLimit, float upperLimit)
        {
            if (lowerLimit >= upperLimit)
                throw new ArgumentException("LowerLimit must be less than UpperLimit");

            LowerLimit = lowerLimit;
            UpperLimit = upperLimit;
        }

        public void UpdateDataType(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("DataType cannot be empty", nameof(dataType));

            DataType = dataType;
        }
        public void UpdateDeadband(int deadband)
        {
            if (deadband < 0)
                throw new ArgumentException("Deadband cannot be negative", nameof(deadband));
            Deadband = deadband;
        }
    }
}