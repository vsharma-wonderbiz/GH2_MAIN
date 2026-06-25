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
    public class ExportRequestTagsConfiguration : IEntityTypeConfiguration<ExportRequestTags>
    {
        public void Configure(EntityTypeBuilder<ExportRequestTags> builder)
        {
            builder.HasKey(a => a.TagRequestId);

            builder.Property(a => a.TagRequestId)
                .ValueGeneratedOnAdd();

            builder.Property(a => a.TagName)
                .IsRequired()
                .HasMaxLength(250);

            builder.HasOne(a => a.ExportRequest)
                .WithMany(u => u.RequestedTags)
                .HasForeignKey(u => u.ExportRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
