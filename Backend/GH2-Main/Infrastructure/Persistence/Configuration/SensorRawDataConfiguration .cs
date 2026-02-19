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
    public class SensorRawDataConfiguration : IEntityTypeConfiguration<SensorRawData>
    {
        public void Configure(EntityTypeBuilder<SensorRawData> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedOnAdd();

            builder.Property(s => s.OpcNodeId).IsRequired();
            builder.Property(s => s.AssetName).IsRequired();
            builder.Property(s => s.TagName).IsRequired();
            builder.Property(s => s.Value).IsRequired();
            builder.Property(s => s.TimeStamp).IsRequired();

          
            builder.HasOne(s => s.Mapping)
                   .WithMany(m => m.SensorData)
                   .HasForeignKey(s => s.MappingId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
