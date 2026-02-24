using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    /// <summary>
    /// Temporary service to backfill SensorRawData with 1-second interval entries.
    /// Uses Modbus simulation logic for realistic value generation.
    /// This is a temporary utility and will be deleted after use.
    /// </summary>
    public class BackfillSensorDataService
    {
        private readonly ApplicationDbContext _context;

        public BackfillSensorDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Backfill 1 month of sensor data for a specific asset and all its tags.
        /// Date range: from current date - 30 days to current date.
        /// Uses Modbus-like simulation logic for value generation.
        /// </summary>
        public async Task BackfillAssetByNameAsync(string assetName)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-1);

            await BackfillAssetAsync(assetName, startDate, endDate);
        }

        /// <summary>
        /// Backfill sensor data for a specific asset in a custom date range.
        /// </summary>
        public async Task BackfillAssetAsync(string assetName, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine($"\n{'='*80}");
            Console.WriteLine($"BACKFILL: Asset '{assetName}' | Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            Console.WriteLine($"{'='*80}\n");

            try
            {
                // Get asset
                var asset = await _context.Assets
                    .Include(a => a.Mappings)
                        .ThenInclude(m => m.Tag)
                    .FirstOrDefaultAsync(a => a.Name == assetName);

                    Console.Write($"these is the asset object :{asset}");

                if (asset == null)
                {
                    Console.WriteLine($"✗ Asset '{assetName}' not found!");
                    return;
                }

                Console.WriteLine($"✓ Found asset: {asset.Name} (AssetId: {asset.AssetId})");

                // Get all tags for this asset
                var mappings = asset.Mappings.ToList();
                if (!mappings.Any())
                {
                    Console.WriteLine($"✗ No tags/mappings found for asset '{assetName}'");
                    return;
                }

                Console.WriteLine($"✓ Found {mappings.Count} tags for this asset\n");

                int totalInserted = 0;
                int tagCount = 0;

                foreach (var mapping in mappings)
                {
                    tagCount++;
                    Console.WriteLine($"[{tagCount}/{mappings.Count}] Processing Tag: {mapping.Tag.TagName}");

                    // Get tag min/max for simulation
                    var tag = mapping.Tag;
                    float min = tag.LowerLimit;
                    float max = tag.UpperLimit;

                    Console.WriteLine($"  Range: {min} - {max}");

                    // Backfill this asset-tag combination
                    int inserted = await BackfillAssetTagAsync(mapping, startDate, endDate, min, max);
                    totalInserted += inserted;

                    Console.WriteLine($"  ✓ Inserted {inserted} records\n");
                }

                Console.WriteLine($"{'='*80}");
                Console.WriteLine($"✓ BACKFILL COMPLETED!");
                Console.WriteLine($"  Asset: {asset.Name}");
                Console.WriteLine($"  Total Records: {totalInserted:N0}");
                Console.WriteLine($"  Date Range: {startDate:O} to {endDate:O}");
                Console.WriteLine($"{'='*80}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ ERROR: {ex.Message}\n");
                throw;
            }
        }

        /// <summary>
        /// Backfill a specific Asset-Tag mapping with simulated data.
        /// </summary>
        private async Task<int> BackfillAssetTagAsync(
            MappingTable mapping, 
            DateTime startDate, 
            DateTime endDate, 
            float minValue, 
            float maxValue)
        {
            try
            {
                var sensorDataList = new List<SensorRawData>();
                var currentValue = (minValue + maxValue) / 2; // Start at midpoint
                var currentTime = startDate;
                int recordCount = 0;

                while (currentTime <= endDate)
                {
                    // Generate simulated value using Modbus-like logic
                    currentValue = SimulateNextValue(currentValue, minValue, maxValue);

                    var sensorData = new SensorRawData
                    {
                        MappingId = mapping.MappingId,
                        OpcNodeId = mapping.OpcNodeId,
                        AssetName = mapping.Asset.Name,
                        TagName = mapping.Tag.TagName,
                        Value = currentValue,
                        TimeStamp = currentTime
                    };

                    sensorDataList.Add(sensorData);
                    recordCount++;

                    currentTime = currentTime.AddSeconds(1);

                    // Bulk insert every 10000 records for optimal performance
                    if (sensorDataList.Count >= 80000)
                    {
                        await _context.SensorRawDatas.AddRangeAsync(sensorDataList);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"    → Inserted {recordCount:N0} records...");
                        sensorDataList.Clear();
                    }
                }

                // Insert remaining records
                if (sensorDataList.Any())
                {
                    await _context.SensorRawDatas.AddRangeAsync(sensorDataList);
                    await _context.SaveChangesAsync();
                }

                return recordCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error backfilling {mapping.Asset.Name} - {mapping.Tag.TagName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Simulate next sensor value using Modbus-like logic.
        /// Adds random delta to create realistic variation.
        /// </summary>
        private float SimulateNextValue(float currentValue, float min, float max)
        {
            var random = new Random();
            
            // Add random delta (±2% of range)
            float delta = (float)random.NextDouble() * 0.04f * (max - min) - 0.02f * (max - min);
            currentValue += delta;

            // Clamp to min/max range
            currentValue = Math.Max(min, Math.Min(currentValue, max));

            // Round to 2 decimal places
            return (float)Math.Round(currentValue, 2);
        }

        /// <summary>
        /// Clear all sensor raw data (for cleanup).
        /// Use with caution!
        /// </summary>
        public async Task ClearSensorDataAsync()
        {
            Console.WriteLine("WARNING: Clearing all SensorRawData records...");
            var allSensorData = await _context.SensorRawDatas.ToListAsync();
            _context.SensorRawDatas.RemoveRange(allSensorData);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Deleted {allSensorData.Count} records");
        }

        /// <summary>
        /// Clear sensor data for a specific asset.
        /// </summary>
        public async Task ClearSensorDataForAssetAsync(string assetName)
        {
            var asset = await _context.Assets
                .Include(a => a.Mappings)
                .FirstOrDefaultAsync(a => a.Name == assetName);

            if (asset == null)
            {
                Console.WriteLine($"Asset '{assetName}' not found");
                return;
            }

            var mappingIds = asset.Mappings.Select(m => m.MappingId).ToList();
            var sensorData = await _context.SensorRawDatas
                .Where(s => mappingIds.Contains(s.MappingId))
                .ToListAsync();

            _context.SensorRawDatas.RemoveRange(sensorData);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Deleted {sensorData.Count} records for asset '{assetName}'");
        }

        /// <summary>
        /// Get statistics about backfill progress.
        /// </summary>
        public async Task<BackfillStatsDto> GetStatsAsync()
        {
            var sensorCount = await _context.SensorRawDatas.CountAsync();
            var mappingCount = await _context.Mappings.CountAsync();

            var sensorRange = await _context.SensorRawDatas
                .OrderBy(s => s.TimeStamp)
                .Select(s => s.TimeStamp)
                .FirstOrDefaultAsync();

            var latestSensor = await _context.SensorRawDatas
                .OrderByDescending(s => s.TimeStamp)
                .Select(s => s.TimeStamp)
                .FirstOrDefaultAsync();

            return new BackfillStatsDto
            {
                TotalMappings = mappingCount,
                TotalSensorRecords = sensorCount,
                EarliestSensorTime = sensorRange,
                LatestSensorTime = latestSensor
            };
        }
    }

    /// <summary>
    /// DTO for backfill statistics.
    /// </summary>
    public class BackfillStatsDto
    {
        public int TotalMappings { get; set; }
        public int TotalSensorRecords { get; set; }
        public DateTime EarliestSensorTime { get; set; }
        public DateTime LatestSensorTime { get; set; }
    }
}
