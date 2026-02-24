using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeding
{
    public class MappingSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Mappings.Any())
                return;

            // Get plant
            var plant = context.Assets
                .FirstOrDefault(a => a.Name == "Plant_1");

            if (plant == null)
                return;

            // Get stack under plant
            var stack = context.Assets
                .FirstOrDefault(a => a.ParentAssetId == plant.AssetId);

            // Get tag type ids
            var plantTypeId = context.TagTypes
                .Where(t => t.TagName == "Plant")
                .Select(t => t.TagTypeId)
                .FirstOrDefault();

            var stackTypeId = context.TagTypes
                .Where(t => t.TagName == "Stack")
                .Select(t => t.TagTypeId)
                .FirstOrDefault();

            // 🔹 1️⃣ Map Plant Level Tags
            var plantTags = context.Tags
                .Where(t => t.TagTypeId == plantTypeId)
                .ToList();

            foreach (var tag in plantTags)
            {
                var opcNodeId = $"ns=2;s={plant.Name}.{tag.TagName}";

                var mapping = new MappingTable(
                    plant.AssetId,
                    tag.TagId,
                    opcNodeId
                );

                context.Mappings.Add(mapping);
            }

            // 🔹 2️⃣ Map Stack Level Tags
            if (stack != null)
            {
                var stackTags = context.Tags
                    .Where(t => t.TagTypeId == stackTypeId)
                    .ToList();

                foreach (var tag in stackTags)
                {
                    var opcNodeId = $"ns=2;s={plant.Name}.{stack.Name}.{tag.TagName}";

                    var mapping = new MappingTable(
                        stack.AssetId,
                        tag.TagId,
                        opcNodeId
                    );

                    context.Mappings.Add(mapping);
                }
            }

            context.SaveChanges();
        }
    }
}