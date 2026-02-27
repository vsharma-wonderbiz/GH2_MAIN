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

            var DerivedTypeId = context.TagTypes
               .Where(a => a.TagName == "Derived")
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
                    new Tag(stackTypeId, "current", "A", 41, 43,"float32",0.06f),
                    new Tag(stackTypeId, "voltage", "V", 44, 44.1f,"float32",0.003f),
                    new Tag(stackTypeId, "pressure", "bar", 25, 30,"float32",0.15f),
                    new Tag(stackTypeId, "outlet_pressure", "bar", 20, 25,"float32",0.15f),
                    new Tag(stackTypeId, "flowrate", "Nm3/h", 2.8f, 3.5f,"float32",0.02f),
                    new Tag(stackTypeId, "temperature", "°C", 48, 55,"float32",0.21f),
                    new Tag(stackTypeId, "h2flow", "Nm3/h", 486, 500,"float32",0.42f),
                    new Tag(stackTypeId, "water_conductivity", "µS/cm", 15, 27,"float32",0.36f),
                    new Tag(stackTypeId, "water_flowrate", "L/min", 25.2f, 33.3f,"float32",0.24f),
                    //new Tag(stackTypeId, "plantdata_water_flow_tot", "L/min", 1, 250),
                    new Tag(stackTypeId, "concentration", "%", 0.5f, 1.0f,"float32",0.015f),
                    new Tag(stackTypeId, "fan_rpm", "RPM", 3810, 4290,"float32",14),
                    new Tag(stackTypeId, "safey_board_temp", "°C", 40, 45,"float32",0.15f),
                    new Tag(stackTypeId, "recombiner_temp", "°C", 55, 65,"float32",0.3f),
                    new Tag(stackTypeId, "downstream_temp", "°C", 50, 60,"float32",0.3f),
                    new Tag(stackTypeId, "5v_board_voltage", "V", 4.99f, 5.024f,"float32",0.001f),
                    new Tag(stackTypeId, "12v_board_voltage", "V", 11.888f, 11.912f,"float32",0.0007f),
                    new Tag(stackTypeId, "24v_board_voltage", "V", 23.407f, 23.649f,"float32",0.007f),
                    new Tag(stackTypeId, "pump_signal", "V", 0.0f, 2.999f,"float32",0.09f),
                    new Tag(stackTypeId, "battery_voltage", "V", 3.011f, 3.06f,"float32",0.0015f),
                    new Tag(stackTypeId, "warning_exists", "bool", 0, 1,"bool",0),
                    new Tag(stackTypeId, "lifetime", "hours", 38000, 40000,"float32",0),
                    new Tag(stackTypeId, "trip_signal", "bool", 0, 1,"float32",0),
                    new Tag(plantTypeId, "power", "kW" ,1000 , 2000 ,"float32",0),
                    new Tag(plantTypeId, "throughput", "Nm3/h" ,200 , 450 ,"float32",0),
                    new Tag(plantTypeId, "water_flow_tot", "m3" ,464 , 465 ,"float32",0),
                    // --------------------
// Plant Level Derived Tags
// --------------------

                   new Tag(plantTypeId, "specific_energy", "kWh/Nm3", 0, 0, "float32", 0,true),
new Tag(plantTypeId, "throughput", "Nm3/h", 0, 0, "float32", 0,true),
new Tag(plantTypeId, "specific_water_consumption", "m3/Nm3", 0, 0, "float32", 0, true),
new Tag(plantTypeId, "inlet_water_conductivity", "µS/cm", 0, 0, "float32", 0, true),

// --------------------
// Stack Level Derived Tags
// --------------------

new Tag(stackTypeId, "stack_specific_energy", "kWh/Nm3", 0, 0, "float32", 0, true),
new Tag(stackTypeId, "throughput", "Nm3/h", 0, 0, "float32", 0, true),
new Tag(stackTypeId, "pressure_diff", "bar", 0, 0, "float32", 0, true),
new Tag(stackTypeId, "voltage_kpi", "V", 0, 0, "float32", 0, true),
new Tag(stackTypeId, "temperature_kpi", "°C", 0, 0, "float32", 0, true),
new Tag(stackTypeId, "ratio", "ratio", 0, 0, "float32", 0, true),
new Tag(stackTypeId, "concentration_kpi", "%", 0, 0, "float32", 0, true),
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