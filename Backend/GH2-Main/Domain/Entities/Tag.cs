using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public TagType TagType { get; private set; }

        public ICollection<MappingTable> Mapings=new List<MappingTable>();

        private Tag() { }

        public Tag(int tagTypeId, string tagName, string unit, float lowerLimit, float upperLimit)
        {
            if (tagTypeId <= 0)
                throw new ArgumentException("TagTypeId must be greater than 0", nameof(tagTypeId));

            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("TagName is required", nameof(tagName));

            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Unit is required", nameof(unit));

            if (lowerLimit >= upperLimit)
                throw new ArgumentException("LowerLimit must be less than UpperLimit");

            this.TagTypeId = tagTypeId;
            this.TagName = tagName;
            this.Unit = unit;
            this.LowerLimit = lowerLimit;
            this.UpperLimit = upperLimit;
        }

        public void UpdateTagName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Asset name cannot be empty");

            this.TagName = newName;
        }

    }
}
