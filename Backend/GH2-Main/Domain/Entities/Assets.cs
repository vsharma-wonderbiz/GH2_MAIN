using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Domain.Entities
{
    public class Assets
    {
        public int AssetId { get; private set; }
        public string Name { get; private set; }
        public string AssetType { get; private set; }
        public int? ParentAssetId { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public ICollection<MappingTable> Mappings { get; private set; } = new List<MappingTable>();

        // EF Core needs a parameterless constructor
        private Assets() { }

        public Assets(string name, int? parentAssetId = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Asset Name is required");

            this.Name = name;
            this.ParentAssetId = parentAssetId;

            // Rule: Plant if no parent, Machine if parent exists
            this.AssetType = parentAssetId == null ? "Plant" : "Stack";
        }

        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Asset name cannot be empty");

            this.Name = newName;
        }

        public void AssignParent(int parentId)
        {
            if (parentId>0)
                throw new ArgumentException("Parent AssetId must be a positive number ");

            this.ParentAssetId = parentId;
            this.AssetType = "Machine";
        }

        public void RemoveParent()
        {
            this.ParentAssetId = null;
            this.AssetType = "Plant";
        }
    }

}
