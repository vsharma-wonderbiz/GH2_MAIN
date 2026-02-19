using Microsoft.Extensions.Logging;
using Domain.Entities;

namespace Infrastructure.Persistence.Seeding
{
    public class TagsSeeder
    {
        public static void Seeder(ApplicationDbContext context, ILogger<TagsSeeder> logger)
        {
            if (!context.TagTypes.Any())
            {
                logger.LogWarning("No TagTypes found in database. Seeding required.");
                return;
            }

            // Get TagType IDs
            var plantTypeId = context.TagTypes
                .Where(a => a.TagName == "Plant")
                .Select(a => a.TagTypeId)
                .FirstOrDefault();

            var stackTypeId = context.TagTypes
                .Where(a => a.TagName == "Stack")
                .Select(a => a.TagTypeId)
                .FirstOrDefault();

            if (stackTypeId == 0)
            {
                logger.LogError("Stack TagType not found. Cannot seed stack tags.");
                return;
            }

            if (!context.Tags.Any(t => t.TagTypeId == stackTypeId))
            {
                logger.LogInformation("Seeding Stack Tags...");

                var stackTags = new List<Tag>
                {
                    new Tag(stackTypeId, "current", "A", 41, 43),
                    new Tag(stackTypeId, "voltage", "V", 44, 44.1f),
                    new Tag(stackTypeId, "pressure", "bar", 25, 30),
                    new Tag(stackTypeId, "outlet_pressure", "bar", 20, 25),
                    new Tag(stackTypeId, "flowrate", "Nm3/h", 2.8f, 3.5f),
                    new Tag(stackTypeId, "temperature", "°C", 48, 55),
                    new Tag(stackTypeId, "h2flow", "Nm3/h", 486, 500),
                    new Tag(stackTypeId, "water_conductivity", "µS/cm", 15, 27),
                    new Tag(stackTypeId, "water_flowrate", "L/min", 25.2f, 33.3f),
                    //new Tag(stackTypeId, "plantdata_water_flow_tot", "L/min", 1, 250),
                    new Tag(stackTypeId, "concentration", "%", 0.5f, 1.0f),
                    new Tag(stackTypeId, "fan_rpm", "RPM", 3810, 4290),
                    new Tag(stackTypeId, "safey_board_temp", "°C", 40, 45),
                    new Tag(stackTypeId, "recombiner_temp", "°C", 55, 65),
                    new Tag(stackTypeId, "downstream_temp", "°C", 50, 60),
                    new Tag(stackTypeId, "5v_board_voltage", "V", 4.99f, 5.024f),
                    new Tag(stackTypeId, "12v_board_voltage", "V", 11.888f, 11.912f),
                    new Tag(stackTypeId, "24v_board_voltage", "V", 23.407f, 23.649f),
                    new Tag(stackTypeId, "pump_signal", "V", 0.0f, 2.999f),
                    new Tag(stackTypeId, "battery_voltage", "V", 3.011f, 3.06f),
                    new Tag(stackTypeId, "warning_exists", "bool", 0, 1),
                    new Tag(stackTypeId, "lifetime", "hours", 38000, 40000),
                    new Tag(stackTypeId, "trip_signal", "bool", 0, 1)
                };

                context.Tags.AddRange(stackTags);
                context.SaveChanges();

                logger.LogInformation("Stack Tags seeded successfully.");
            }
            else
            {
                logger.LogInformation("Stack Tags already exist. Skipping seeding.");
            }
        }
    }
}