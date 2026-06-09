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
    public class RecommendationConfiguration : IEntityTypeConfiguration<RecommendationInfo>
    {
        public void Configure(EntityTypeBuilder<RecommendationInfo> entity)
        {
            entity.HasKey(x => x.Id);

            entity.Property(a => a.Id)
                   .ValueGeneratedOnAdd();

            entity.Property(a => a.SignalName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasOne(a => a.Mapping)        // AlarmInfo has one Mapping
                  .WithMany(m => m.Recommendations)       // Mapping has many AlarmInfos
                  .HasForeignKey(a => a.MappingId) // FK column
                  .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
