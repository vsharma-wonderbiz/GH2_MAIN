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

            var stacks = context.Assets
                .Where(a => a.ParentAssetId == plant.AssetId)
                .ToList();

            var plantTypeId = context.TagTypes
                .Where(t => t.TagName == "Plant")
                .Select(t => t.TagTypeId)
                .FirstOrDefault();

            var stackTypeId = context.TagTypes
                .Where(t => t.TagName == "Stack")
                .Select(t => t.TagTypeId)
                .FirstOrDefault();

            var plantPhysicalTags = context.Tags
                .Where(t => t.TagTypeId == plantTypeId && !t.IsDerived)
                .ToList();

            var stackPhysicalTags = context.Tags
                .Where(t => t.TagTypeId == stackTypeId && !t.IsDerived)
                .ToList();

            var plantDerivedTags = context.Tags
                .Where(t => t.TagTypeId == plantTypeId && t.IsDerived)
                .ToList();

            var stackDerivedTags = context.Tags
                .Where(t => t.TagTypeId == stackTypeId && t.IsDerived)
                .ToList();

            foreach (var tag in plantPhysicalTags)
            {
                var opcNodeId = $"ns=2;s={plant.Name}.{tag.TagName}";

                context.Mappings.Add(new MappingTable(
                    plant.AssetId,
                    tag.TagId,
                    opcNodeId
                ));
            }

            foreach (var stack in stacks)
            {
                foreach (var tag in stackPhysicalTags)
                {
                    var opcNodeId = $"ns=2;s={plant.Name}.{stack.Name}.{tag.TagName}";

                    context.Mappings.Add(new MappingTable(
                        stack.AssetId,
                        tag.TagId,
                        opcNodeId
                    ));
                }
            }

            foreach (var tag in plantDerivedTags)
            {
                context.Mappings.Add(new MappingTable(
                    plant.AssetId,
                    tag.TagId,
                    null
                ));
            }

            foreach (var stack in stacks)
            {
                foreach (var tag in stackDerivedTags)
                {
                    context.Mappings.Add(new MappingTable(
                        stack.AssetId,
                        tag.TagId,
                        null
                    ));
                }
            }

            context.SaveChanges();
        }
    }
}