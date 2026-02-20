using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Persistence.Configuration
{
    public class ProtocolConfiguration : IEntityTypeConfiguration<ProtocolConfig>
    {
        public void Configure(EntityTypeBuilder<ProtocolConfig> builder)
        {
            builder.HasKey(u=>u.Id);
            builder.Property(u => u.Id).ValueGeneratedOnAdd();
            builder.Property(u => u.RegisterAddress).IsRequired();
            builder.Property(u=>u.SlaveId).IsRequired();
            builder.Property(u=>u.RegisterCount).IsRequired();

            builder.HasOne(u=>u.Mapping)
                .WithMany(u=>u.ModbusConifg)
                .HasForeignKey(u=>u.MappingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
