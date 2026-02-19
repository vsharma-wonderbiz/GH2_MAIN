using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Infrastructure.Persistence
{
    public  class TagTypeSeeder
    {
        public static void Seeder(ApplicationDbContext context)
        {
            if(!context.TagTypes.Any())
            {
                context.TagTypes.AddRange(
                    new TagType("Plant"),
                    new TagType("Stack")
                    );

                context.SaveChanges();
            }
        }
    }
}
