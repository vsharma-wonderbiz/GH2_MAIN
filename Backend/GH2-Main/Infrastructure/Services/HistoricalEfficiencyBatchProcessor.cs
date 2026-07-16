using Domain.Entities;
using Domain.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Services
{
    public class HistoricalEfficiencyBatchProcessor
    {
        private readonly ApplicationDbContext _context;
        private readonly EfficiencyCalculationEngine _engine = new();

        private const double TickSeconds = 5.0;       // must match the backfill's raw tick size
        private const int PointsPerHourTarget = 12;    // <- the knob: how many samples/hour we want

        // ticks/hour = 3600/TickSeconds = 720. Sampling every Nth row:
        // 720 / PointsPerHourTarget = 60 -> take every 60th row.
        private static readonly int SampleEveryNthRow = (int)(3600.0 / TickSeconds) / PointsPerHourTarget;

        // Actual elapsed time between two consecutive PROCESSED samples,
        // not between raw ticks. This must match what we actually sample,
        // or OperationalHours (and everything derived from it) drifts.
        private static readonly double DtHours = SampleEveryNthRow * TickSeconds / 3600.0;

        private const int BatchSize = 5000;

        public HistoricalEfficiencyBatchProcessor(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProcessAssetHistoryAsync(string assetName)
        {
            var state = new StackProcessingState { AssetName = assetName }; // fresh start: BOL, 0 hours

            var connectionString = _context.Database.GetConnectionString()
                ?? throw new InvalidOperationException("No connection string configured on the DbContext.");

            // --- THE READ SIDE ---
            // Step 1 (pivoted CTE): same pivot as before - one row per timestamp,
            // built with conditional aggregation instead of C#-side GroupBy.
            //
            // Step 2 (numbered CTE): ROW_NUMBER() over the pivoted, already-
            // ordered rows. This costs nothing extra since Postgres already
            // has them sorted for the GROUP BY/ORDER BY above.
            //
            // Step 3 (outer SELECT): keep only every Nth row (rn % N = 1),
            // so we get exactly PointsPerHourTarget rows per hour of raw data.
            // Postgres does the reduction - we never pull 720 rows/hour into
            // .NET only to throw away 708 of them.
            //
            // NOTE: still assumes an index on ("AssetName", "TagName", "TimeStamp")
            // in "SensorRawDatas" - matters even more now since we're scanning
            // all raw rows just to discard most of them at the DB layer.
            const string pivotSql = @"
                WITH pivoted AS (
                    SELECT
                        ""TimeStamp"",
                        MAX(CASE WHEN ""TagName"" = 'current'     THEN ""Value"" END) AS current_val,
                        MAX(CASE WHEN ""TagName"" = 'voltage'     THEN ""Value"" END) AS voltage_val,
                        MAX(CASE WHEN ""TagName"" = 'temperature' THEN ""Value"" END) AS temperature_val,
                        MAX(CASE WHEN ""TagName"" = 'pressure'    THEN ""Value"" END) AS pressure_val,
                        MAX(CASE WHEN ""TagName"" = 'h2flow'      THEN ""Value"" END) AS h2flow_val
                    FROM ""SensorRawDatas""
                    WHERE ""AssetName"" = @assetName
                      AND ""TagName"" IN ('current', 'voltage', 'temperature', 'pressure', 'h2flow')
                    GROUP BY ""TimeStamp""
                ),
                numbered AS (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY ""TimeStamp"") AS rn
                    FROM pivoted
                )
                SELECT ""TimeStamp"", current_val, voltage_val, temperature_val, pressure_val, h2flow_val
                FROM numbered
                WHERE rn % @sampleEveryNthRow = 1
                ORDER BY ""TimeStamp"";";

            await using var readConnection = new NpgsqlConnection(connectionString);
            await readConnection.OpenAsync();

            await using var command = new NpgsqlCommand(pivotSql, readConnection);
            command.Parameters.AddWithValue("assetName", assetName);
            command.Parameters.AddWithValue("sampleEveryNthRow", SampleEveryNthRow);

            await using var reader = await command.ExecuteReaderAsync();

            int totalProcessed = 0;
            var buffer = new List<StackEfficiencyRecord>(BatchSize);

            while (await reader.ReadAsync())
            {
                if (await reader.IsDBNullAsync(1) || await reader.IsDBNullAsync(2) ||
                    await reader.IsDBNullAsync(3) || await reader.IsDBNullAsync(4) ||
                    await reader.IsDBNullAsync(5))
                {
                    continue;
                }

                var timestamp = reader.GetDateTime(0);
                var current = reader.GetDouble(1);
                var voltage = reader.GetDouble(2);
                var temperature = reader.GetDouble(3);
                var pressure = reader.GetDouble(4);
                var h2flow = reader.GetDouble(5);

                var snapshot = new RawSignalSnapshot(current, voltage, temperature, pressure, h2flow, timestamp);

                // DtHours now reflects the real gap between processed samples
                // (5 min), not the raw tick size (5 sec).
                buffer.Add(_engine.ProcessTick(state, snapshot, DtHours));
                totalProcessed++;

                if (buffer.Count >= BatchSize)
                {
                    await SaveBatchAsync(buffer);
                    buffer.Clear();
                    Console.WriteLine($"[{assetName}] Historical batch processed: {totalProcessed:N0} ticks so far...");
                }
            }

            if (buffer.Count > 0)
                await SaveBatchAsync(buffer);

            Console.WriteLine($"[{assetName}] Historical processing complete: {totalProcessed:N0} ticks. " +
                               $"Final tracked efficiency={state.TrackedEfficiency:F2}%, operationalHours={state.OperationalHours:F1}h");
        }

        private async Task SaveBatchAsync(List<StackEfficiencyRecord> buffer)
        {
            _context.StackEfficiencyRecords.AddRange(buffer);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }
    }
}