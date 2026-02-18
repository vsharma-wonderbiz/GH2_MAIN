using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Assets
    {
        public int AssetId { get; set; }

       
        public required string Name { get; set; }

        public required string AssetType { get; set; }

        public string? ParentAssetId { get; set; }

        public DateTime CreatedAt { get; set; }= DateTime.Now;
    }
}
