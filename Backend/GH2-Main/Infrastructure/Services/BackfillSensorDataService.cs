using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Collections.Concurrent;

namespace Infrastructure.Services
{
    public class BackfillSensorDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly HistoricalEfficiencyBatchProcessor _batchProcessor;
        private readonly ModbusStateUpdater _modbusStateUpdater;
        private readonly SeedingGate _seedingGate;

        private const float RATED_LIFETIME_HOURS = 40000f;
        private const float BASE_DEGRADATION_RATE_PERCENT = 0.5f; // % per 1000 operating hours

        private static readonly float LifetimeLostPerOperatingHour =
            (BASE_DEGRADATION_RATE_PERCENT / 100f) * RATED_LIFETIME_HOURS / 1000f;

     
        private const float H2_EFFICIENCY_START_PERCENT = 80f;
        private const float H2_EFFICIENCY_END_PERCENT = 60f;
        private static readonly double H2TheoreticalNm3PerHour = 0.000418 * 42 * 28;

        private const int MaxParallelTags = 8;

        public BackfillSensorDataService(
            ApplicationDbContext context,
            HistoricalEfficiencyBatchProcessor batchProcessor,
            ModbusStateUpdater modbusStateUpdater,
            SeedingGate seedingGate)
        {
            _context = context;
            _batchProcessor = batchProcessor;
            _modbusStateUpdater = modbusStateUpdater;
            _seedingGate = seedingGate;
        }

        public async Task BackfillAssetAsync(string assetName)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddYears(-2);

            var asset = await _context.Assets
                .Include(a => a.Mappings)
                .ThenInclude(m => m.Tag)
                .FirstOrDefaultAsync(a => a.Name == assetName);

            if (asset == null)
            {
                Console.WriteLine($"Asset '{assetName}' not found.");
                return;
            }

            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("No connection string configured on the DbContext.");

            var tagsToProcess = asset.Mappings
                .Where(m => m.Tag != null && !m.Tag.IsDerived)
                .ToList();

            var options = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelTags };

            var finalValues = new ConcurrentDictionary<string, float>();

            await Parallel.ForEachAsync(tagsToProcess, options, async (mapping, _) =>
            {
                Console.WriteLine($"Processing {mapping.Tag!.TagName}");

                float finalValue = await BackfillWithBulkCopyAsync(
                    connectionString,
                    mapping,
                    startDate,
                    endDate,
                    mapping.Tag.LowerLimit,
                    mapping.Tag.UpperLimit);

                finalValues[mapping.Tag.TagName.ToLowerInvariant()] = finalValue;
            });

            if (asset.AssetType == "Stack")
            {

                await _batchProcessor.ProcessAssetHistoryAsync(assetName);
            }
            
            if (finalValues.TryGetValue("lifetime", out var finalLifetime) &&
                finalValues.TryGetValue("h2flow", out var finalH2Flow))
            {
                await _modbusStateUpdater.UpdateStackFinalStateAsync(assetName, finalLifetime, finalH2Flow);
                Console.WriteLine($"[{assetName}] Modbus state.json updated with final lifetime={finalLifetime:F2}, h2flow={finalH2Flow:F3}");
            }
            else
            {
                Console.WriteLine($"[{assetName}] Warning: lifetime or h2flow tag not found - state.json not updated.");
            }

            await CheckAndSignalIfFullySeededAsync();
        }

        private async Task<float> BackfillWithBulkCopyAsync(
            string connectionString,
            MappingTable mapping,
            DateTime startDate,
            DateTime endDate,
            float minValue,
            float maxValue)
        {
            if (mapping.Asset == null)
                throw new ArgumentNullException(nameof(mapping), "Asset not found in mapping");
            if (mapping.Tag == null)
                throw new ArgumentNullException(nameof(mapping), "Tag not found in mapping");

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            bool isLifetimeTag = mapping.Tag.TagName.Equals("lifetime", StringComparison.OrdinalIgnoreCase);
            bool isH2FlowTag = mapping.Tag.TagName.Equals("h2flow", StringComparison.OrdinalIgnoreCase);

            const double tickSeconds = 900;
            double elapsedOperatingHours = 0.0;

            float currentValue = isLifetimeTag ? RATED_LIFETIME_HOURS
                                : isH2FlowTag ? ComputeH2FlowValue(0.0)
                                : (minValue + maxValue) / 2;

            var currentTime = startDate;
            int totalCount = 0;
            const int batchSize = 100000;

            var assetName = mapping.Asset.Name;
            var tagName = mapping.Tag.TagName;

            while (currentTime <= endDate)
            {
                using var writer = await connection.BeginBinaryImportAsync(
                    "COPY \"SensorRawDatas\" (\"MappingId\", \"OpcNodeId\", \"AssetName\", \"TagName\", \"Value\", \"TimeStamp\") FROM STDIN (FORMAT BINARY)");

                int batchCount = 0;

                while (currentTime <= endDate && batchCount < batchSize)
                {
                    elapsedOperatingHours += tickSeconds / 3600.0;

                    if (isLifetimeTag)
                    {
                        double preciseLifetime =
                            RATED_LIFETIME_HOURS - elapsedOperatingHours;
                        preciseLifetime = Math.Max(0.0, preciseLifetime);
                        currentValue = (float)Math.Round(preciseLifetime, 4);
                    }
                    else if (isH2FlowTag)
                    {
                        currentValue = ComputeH2FlowValue(elapsedOperatingHours);
                    }
                    else
                    {
                        currentValue = SimulateNextValue(currentValue, minValue, maxValue);
                    }

                    writer.StartRow();
                    writer.Write(mapping.MappingId, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(mapping.OpcNodeId, NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(assetName, NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(tagName, NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(currentValue, NpgsqlTypes.NpgsqlDbType.Real);
                    writer.Write(currentTime, NpgsqlTypes.NpgsqlDbType.TimestampTz);

                    totalCount++;
                    batchCount++;
                    currentTime = currentTime.AddSeconds(tickSeconds);
                }

                await writer.CompleteAsync();
                Console.WriteLine($"[{tagName}] Inserted {totalCount:N0} rows...");
            }

            Console.WriteLine($"[{tagName}] Finished: {totalCount:N0} rows inserted.");

           
            return currentValue;
        }

        private static float ComputeH2FlowValue(double elapsedOperatingHours)
        {
            double lifetimeFraction = Math.Min(elapsedOperatingHours / RATED_LIFETIME_HOURS, 1.0);

            double efficiencyPercent = H2_EFFICIENCY_START_PERCENT
                - (H2_EFFICIENCY_START_PERCENT - H2_EFFICIENCY_END_PERCENT) * lifetimeFraction;

            double h2ActualNm3PerHour = (efficiencyPercent / 100.0) * H2TheoreticalNm3PerHour;

            return (float)Math.Round(h2ActualNm3PerHour * 1000.0, 3);
        }

        private static float SimulateNextValue(float currentValue, float min, float max)
        {
            float delta = (float)Random.Shared.NextDouble() * 0.04f * (max - min)
                          - 0.02f * (max - min);

            currentValue += delta;
            currentValue = Math.Max(min, Math.Min(currentValue, max));

            return (float)Math.Round(currentValue, 2);
        }


        private async Task CheckAndSignalIfFullySeededAsync()
        {
            if (_seedingGate.IsReady)
                return; // already signalled, nothing to do

            var totalAssetCount = await _context.Assets.Where(a=>a.AssetType=="Stack").CountAsync();

            var seededAssetCount = await _context.StackEfficiencyRecords
                .Select(r => r.AssetName)
                .Distinct()
                .CountAsync();

            Console.WriteLine($"[Seeding Check] {seededAssetCount}/{totalAssetCount} assets seeded so far.");

            if (totalAssetCount > 0 && seededAssetCount >= totalAssetCount)
            {
                Console.WriteLine("[Seeding Check] All assets seeded - opening the gate for background processing.");
                _seedingGate.SignalReady();
            }
        }
    }
}