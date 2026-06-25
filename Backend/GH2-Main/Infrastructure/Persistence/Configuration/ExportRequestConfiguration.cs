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
    public  class ExportRequestConfiguration : IEntityTypeConfiguration<ExportRequest>
    {
        public void Configure(EntityTypeBuilder<ExportRequest> builder)
        {
            builder.HasKey(a=>a.ExportRequestId);

            builder.Property(a => a.ExportRequestId)
                      .ValueGeneratedOnAdd();

            builder.Property(a => a.AssetName)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(a => a.Status)
               .IsRequired()
               .HasMaxLength(250);

            builder.Property(a => a.RequestedBy)
               .IsRequired()
               .HasMaxLength(250);

            builder.Property(a => a.StartTime)
               .HasColumnType("timestamptz");

            builder.Property(a => a.EndTime)
              .HasColumnType("timestamptz");
        }
    }
}
