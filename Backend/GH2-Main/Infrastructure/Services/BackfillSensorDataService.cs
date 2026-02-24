using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

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

        public async Task BackfillAssetAsync(string assetName, DateTime startDate, DateTime endDate)
        {
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

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = "SensorRawDatas",
                BatchSize = 100000,
                BulkCopyTimeout = 0
            };

            bulkCopy.ColumnMappings.Add("MappingId", "MappingId");
            bulkCopy.ColumnMappings.Add("OpcNodeId", "OpcNodeId");
            bulkCopy.ColumnMappings.Add("AssetName", "AssetName");
            bulkCopy.ColumnMappings.Add("TagName", "TagName");
            bulkCopy.ColumnMappings.Add("Value", "Value");
            bulkCopy.ColumnMappings.Add("TimeStamp", "TimeStamp");

            var table = CreateDataTable();

            float currentValue = (minValue + maxValue) / 2;
            var currentTime = startDate;
            int totalCount = 0;

            var assetName = mapping.Asset.Name;
            var tagName = mapping.Tag.TagName;

            while (currentTime <= endDate)
            {
                currentValue = SimulateNextValue(currentValue, minValue, maxValue);

                table.Rows.Add(
                    mapping.MappingId,
                    mapping.OpcNodeId,
                    assetName,
                    tagName,
                    currentValue,
                    currentTime);

                totalCount++;
                currentTime = currentTime.AddSeconds(1);

                if (table.Rows.Count >= 100000)
                {
                    await bulkCopy.WriteToServerAsync(table);
                    Console.WriteLine($"Inserted {totalCount:N0} rows...");
                    table.Clear();
                }
            }

            if (table.Rows.Count > 0)
            {
                await bulkCopy.WriteToServerAsync(table);
                table.Clear();
            }

            Console.WriteLine($"Finished: {totalCount:N0} rows inserted.");
        }

        private DataTable CreateDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("MappingId", typeof(int));
            table.Columns.Add("OpcNodeId", typeof(string));
            table.Columns.Add("AssetName", typeof(string));
            table.Columns.Add("TagName", typeof(string));
            table.Columns.Add("Value", typeof(float));
            table.Columns.Add("TimeStamp", typeof(DateTime));
            return table;
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