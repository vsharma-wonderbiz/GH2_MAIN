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

            var plant = context.Assets
                .FirstOrDefault(a => a.Name == "Plant_1");

            if (plant == null)
                return;

            var stack = context.Assets
                .FirstOrDefault(a => a.ParentAssetId == plant.AssetId);

            var plantTypeId = context.TagTypes
                .Where(t => t.TagName == "Plant")
                .Select(t => t.TagTypeId)
                .FirstOrDefault();

            var stackTypeId = context.TagTypes
                .Where(t => t.TagName == "Stack")
                .Select(t => t.TagTypeId)
                .FirstOrDefault();

            var derivedTypeId = context.TagTypes
                .Where(t => t.TagName == "Derived")
                .Select(t => t.TagTypeId)
                .FirstOrDefault();

            // 🔹 1️⃣ Plant Physical Tags
            var plantTags = context.Tags
                .Where(t => t.TagTypeId == plantTypeId)
                .ToList();

            foreach (var tag in plantTags)
            {
                var opcNodeId = $"ns=2;s={plant.Name}.{tag.TagName}";

                context.Mappings.Add(new MappingTable(
                    plant.AssetId,
                    tag.TagId,
                    opcNodeId
                ));
            }

            // 🔹 2️⃣ Stack Physical Tags
            if (stack != null)
            {
                var stackTags = context.Tags
                    .Where(t => t.TagTypeId == stackTypeId)
                    .ToList();

                foreach (var tag in stackTags)
                {
                    var opcNodeId = $"ns=2;s={plant.Name}.{stack.Name}.{tag.TagName}";

                    context.Mappings.Add(new MappingTable(
                        stack.AssetId,
                        tag.TagId,
                        opcNodeId
                    ));
                }
            }

            // 🔹 3️⃣ Derived Tags (NO OPC NODE)
            var derivedTags = context.Tags
                .Where(t => t.TagTypeId == derivedTypeId)
                .ToList();

            foreach (var tag in derivedTags)
            {
                // Determine level from name
                if (tag.TagName.StartsWith("plant_derived"))
                {
                    context.Mappings.Add(new MappingTable(
                        plant.AssetId,
                        tag.TagId,
                        null   // no OPC
                    ));
                }
                else if (tag.TagName.StartsWith("stack_derived") && stack != null)
                {
                    context.Mappings.Add(new MappingTable(
                        stack.AssetId,
                        tag.TagId,
                        null   // no OPC
                    ));
                }
            }

            context.SaveChanges();
        }
    }
}