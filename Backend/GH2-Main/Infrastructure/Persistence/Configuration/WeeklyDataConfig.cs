using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Persistence.Configurations
{
    public class MappingTableConfiguration : IEntityTypeConfiguration<MappingTable>
    {
        public void Configure(EntityTypeBuilder<MappingTable> builder)
        {
            // Table name
            builder.ToTable("MappingTables");

            // Primary key
            builder.HasKey(m => m.MappingId);

            builder.Property(m => m.MappingId)
                   .ValueGeneratedOnAdd();

            // Foreign keys
            builder.HasOne(m => m.Asset)
                   .WithMany(a => a.Mappings) // assumes Assets entity has ICollection<MappingTable> Mappings
                   .HasForeignKey(m => m.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.Tag)
                   .WithMany(t => t.Mappings) // assumes Tag entity has ICollection<MappingTable> Mappings
                   .HasForeignKey(m => m.TagId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Properties
            builder.Property(m => m.OpcNodeId)
                   .HasMaxLength(255) // optional length constraint
                   .IsUnicode(false);

            builder.Property(m => m.CreatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .ValueGeneratedOnAdd();

            // Relationships with child collections
            builder.HasMany(m => m.SensorData)
                   .WithOne(sd => sd.Mapping)
                   .HasForeignKey(sd => sd.MappingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.NodeLastData)
                   .WithOne(nd => nd.Mapping)
                   .HasForeignKey(nd => nd.MappingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.TransactionData)
                   .WithOne(td => td.Mapping)
                   .HasForeignKey(td => td.MappingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.ModbusConifg)
                   .WithOne(pc => pc.Mapping)
                   .HasForeignKey(pc => pc.MappingId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Indexes for faster lookups
            builder.HasIndex(m => m.AssetId);
            builder.HasIndex(m => m.TagId);
            builder.HasIndex(m => m.OpcNodeId);
        }
    }
}