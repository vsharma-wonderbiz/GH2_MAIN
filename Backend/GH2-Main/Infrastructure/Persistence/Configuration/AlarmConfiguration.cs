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
    public class AlarmConfiguration : IEntityTypeConfiguration<AlarmInfo>
    {
        public void Configure(EntityTypeBuilder<AlarmInfo> entity)
        {

        }

    }
}
