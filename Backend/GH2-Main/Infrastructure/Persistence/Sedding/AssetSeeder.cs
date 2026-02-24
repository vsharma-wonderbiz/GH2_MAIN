
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Sedding
{
    public class AssetSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Assets.Any(a => a.Name == "Plant_1"))
                return;

            var plant = new Assets("Plant_1");
            context.Assets.Add(plant);
            context.SaveChanges();

            var stack = new Assets("Stack_1", plant.AssetId);
            context.Assets.Add(stack);
            context.SaveChanges();
        }
    }
}
