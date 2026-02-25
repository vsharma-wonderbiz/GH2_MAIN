using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Services
{
    public class BackfillSensorDataService
    {
        private readonly ApplicationDbContext _context;
        private static readonly Random _random = new Random();

        public BackfillSensorDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task BackfillAssetAsync(string assetName)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-1);

            var asset = await _context.Assets
                .Include(a => a.Mappings)
                .ThenInclude(m => m.Tag)
                .FirstOrDefaultAsync(a => a.Name == assetName);

            if (asset == null)
            {
                Console.WriteLine($"Asset '{assetName}' not found.");
                return;
            }

            foreach (var mapping in asset.Mappings)
            {
                Console.WriteLine($"Processing {mapping.Tag.TagName}");
                await BackfillWithBulkCopyAsync(
                    mapping,
                    startDate,
                    endDate,
                    mapping.Tag.LowerLimit,
                    mapping.Tag.UpperLimit);
            }
        }

        private async Task BackfillWithBulkCopyAsync(
            MappingTable mapping,
            DateTime startDate,
            DateTime endDate,
            float minValue,
            float maxValue)
        {
            var connectionString = _context.Database.GetConnectionString();

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            float currentValue = (minValue + maxValue) / 2;
            var currentTime = startDate;
            int totalCount = 0;
            int batchSize = 100000;

            var assetName = mapping.Asset.Name;
            var tagName = mapping.Tag.TagName;

            // PostgreSQL COPY is one continuous stream — we'll restart it per batch
            while (currentTime <= endDate)
            {
                // Begin a COPY stream for each batch
                using var writer = await connection.BeginBinaryImportAsync(
                    "COPY \"SensorRawDatas\" (\"MappingId\", \"OpcNodeId\", \"AssetName\", \"TagName\", \"Value\", \"TimeStamp\") FROM STDIN (FORMAT BINARY)");

                int batchCount = 0;

                while (currentTime <= endDate && batchCount < batchSize)
                {
                    currentValue = SimulateNextValue(currentValue, minValue, maxValue);

                    await writer.StartRowAsync();
                    await writer.WriteAsync(mapping.MappingId, NpgsqlTypes.NpgsqlDbType.Integer);
                    await writer.WriteAsync(mapping.OpcNodeId, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(assetName, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(tagName, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(currentValue, NpgsqlTypes.NpgsqlDbType.Real);
                    await writer.WriteAsync(currentTime, NpgsqlTypes.NpgsqlDbType.Timestamp);

                    totalCount++;
                    batchCount++;
                    currentTime = currentTime.AddSeconds(1);
                }

                // Complete the COPY stream — this is what actually commits the batch
                await writer.CompleteAsync();
                Console.WriteLine($"Inserted {totalCount:N0} rows...");
            }

            Console.WriteLine($"Finished: {totalCount:N0} rows inserted.");
        }

        private float SimulateNextValue(float currentValue, float min, float max)
        {
            float delta = (float)_random.NextDouble() * 0.04f * (max - min)
                          - 0.02f * (max - min);

            currentValue += delta;
            currentValue = Math.Max(min, Math.Min(currentValue, max));

            return (float)Math.Round(currentValue, 2);
        }
    }
}