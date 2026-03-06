using System;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration
{
    public class KpiTableConfiguration : IEntityTypeConfiguration<KpiTable>
    {
        public void Configure(EntityTypeBuilder<KpiTable> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.KpiName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.AssetName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Level)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.KpiValue)
                   .IsRequired();

            builder.Property(x => x.StartTime)
                   .IsRequired();

            builder.Property(x => x.EndTime)
                   .IsRequired();

            builder.Property(x => x.CalculatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}