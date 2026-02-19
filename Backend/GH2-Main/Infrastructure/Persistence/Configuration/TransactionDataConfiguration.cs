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
    public class TransactionDataConfiguration : IEntityTypeConfiguration<TransactionData>
    {
        public void Configure(EntityTypeBuilder<TransactionData> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedOnAdd();

            builder.Property(s => s.OpcNodeId).IsRequired();
            builder.Property(s => s.AssetName).IsRequired();
            builder.Property(s => s.TagName).IsRequired();
            builder.Property(s => s.Value).IsRequired();
            builder.Property(s => s.TimeStamp).IsRequired();


            builder.HasOne(s => s.Mapping)
                   .WithMany(m => m.TrnasactionData)
                   .HasForeignKey(s => s.MappingId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
