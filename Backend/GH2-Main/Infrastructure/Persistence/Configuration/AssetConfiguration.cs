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
    public class AssetConfiguration : IEntityTypeConfiguration<Assets>
    {
        public void Configure(EntityTypeBuilder<Assets> builder)
        {
            builder.HasKey(a => a.AssetId);

            
            builder.Property(a => a.AssetId)
                   .ValueGeneratedOnAdd();

       
            builder.Property(a => a.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasMany(a => a.Mappings)
                   .WithOne(m => m.Asset)
                   .HasForeignKey(m => m.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);

        }

    }
}
