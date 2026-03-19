using System;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Sedding
{
    public class ProtocolDataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Prevent duplicate insert
            if (await context.ProtocolConfig.AnyAsync())
                return;

            // Fetch all mappings with Tag & Stack
            var mappings = await context.Mappings
                .Include(m => m.Tag)
                .Include(m => m.Asset)
                .Where(a=>a.Tag.IsDerived==false && a.Asset.Name=="Stack_1")
                .ToListAsync();

            foreach (var mapping in mappings)
            {
                int registerAddress = GetRegisterAddress(mapping.Tag.TagName);

                // Convert 0 -> 40001
                int modbusAddress = 40001 + registerAddress;

                var protocol = new ProtocolConfig(mapping.MappingId, modbusAddress,2, 3, 1);
             
                await context.ProtocolConfig.AddAsync(protocol);
            }

            await context.SaveChangesAsync();
        }

        private static int GetRegisterAddress(string tagName)
        {
            var registerMap = new Dictionary<string, int>
            {
     { "current", 0 },
    { "voltage", 2 },
    { "pressure", 4 },
    { "outlet_pressure", 6 },
    { "flowrate", 8 },
    { "temperature", 10 },
    { "h2flow", 12 },
    { "water_conductivity", 14 },
    { "water_flowrate", 16 },
    //{ "plantdata_water_flow_tot", 18 },
    { "concentration", 18 },
    { "fan_rpm", 20 },
    { "safey_board_temp", 22 },
    { "recombiner_temp", 24 },
    { "downstream_temp", 26 },
    { "5v_board_voltage", 28 },
    { "12v_board_voltage", 30 },
    { "24v_board_voltage", 32 },
    { "pump_signal", 34 },
    { "battery_voltage", 36 },
    { "warning_exists", 38 },
    { "lifetime", 40 },
    { "trip_signal", 42 },
    { "power", 460 },
    { "throughput", 462 },
    { "water_flow_tot", 464 }
            };

            return registerMap.TryGetValue(tagName, out var address)
                ? address
                : 0;
        }
    }
}