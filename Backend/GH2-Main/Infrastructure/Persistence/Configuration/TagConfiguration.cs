using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configuration
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.HasKey(t => t.TagId);
            builder.Property(t => t.TagId).ValueGeneratedOnAdd();

            builder.Property(t => t.TagName).IsRequired();
            builder.Property(t => t.Unit).IsRequired();

            
            builder.HasOne(t => t.TagType)          
                   .WithMany(tt => tt.Tags)       
                   .HasForeignKey(t => t.TagTypeId) 
                   .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
