using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration
{
    public class MappingTableConfiguration : IEntityTypeConfiguration<MappingTable>
    { 
       public void Configure(EntityTypeBuilder<MappingTable> builder)
        {
            builder.HasKey(m => m.MappingId);
            builder.Property(m => m.MappingId).ValueGeneratedOnAdd();

            builder.Property(m => m.OpcNodeId).IsRequired();
            builder.Property(m => m.DataType).IsRequired();
            builder.Property(m => m.RegisterAddress).IsRequired();
            builder.Property(m => m.RegisterCount).IsRequired();
            builder.Property(m => m.FunctionCode).IsRequired();
            builder.Property(m => m.SlaveId).IsRequired();

            
            builder.HasOne(m => m.Asset)
                   .WithMany(a => a.Mappings)
                   .HasForeignKey(m => m.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);

           
            builder.HasOne(m => m.Tag)
                   .WithMany(t => t.Mapings)
                   .HasForeignKey(m => m.TagId)
                   .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
