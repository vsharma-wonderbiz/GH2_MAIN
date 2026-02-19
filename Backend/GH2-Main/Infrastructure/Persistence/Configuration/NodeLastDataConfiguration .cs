using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Configuration
{
    public class NodeLastDataConfiguration : IEntityTypeConfiguration<NodeLastData>
    {
        public void Configure(EntityTypeBuilder<NodeLastData> builder)
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id).ValueGeneratedOnAdd();

            builder.Property(n => n.OpcNodeId).IsRequired();
            builder.Property(n => n.AssetName).IsRequired();
            builder.Property(n => n.TagName).IsRequired();
            builder.Property(n => n.Value).IsRequired();
            builder.Property(n => n.TimeStamp).IsRequired();

            // Relationship: NodeLastData → MappingTable
            builder.HasOne(n => n.Mapping)
                   .WithMany(m => m.NodeLastData)
                   .HasForeignKey(n => n.MappingId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
