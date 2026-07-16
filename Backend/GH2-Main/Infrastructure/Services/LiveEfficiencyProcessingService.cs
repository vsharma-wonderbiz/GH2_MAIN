using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class LiveEfficiencyProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SeedingGate _seedingGate;
        private readonly ILogger<LiveEfficiencyProcessingService> _logger;
        private readonly EfficiencyCalculationEngine _engine = new();
        private readonly Dictionary<string, StackProcessingState> _stateByAsset = new();

        private const double DtHours = 5.0 / 3600.0;
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

        public LiveEfficiencyProcessingService(IServiceProvider serviceProvider, ILogger<LiveEfficiencyProcessingService> logger, SeedingGate seedingGate)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _seedingGate = seedingGate;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background service started - waiting for seeding to complete before processing...");

            // Blocks here until SeedingGate.SignalReady() is called (either by
            // the backfill service after the last asset, or by the startup
            // check in Program.cs if data already existed from a prior run).
            await _seedingGate.WaitUntilReadyAsync(stoppingToken);

            _logger.LogInformation("Seeding complete signal received. First processing run will trigger in {Minutes} minutes.",
                PollInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait BEFORE processing - this applies to the very first
                // iteration too, so there's always a 5-minute gap between the
                // gate opening (or the previous run) and the next trigger.
                await Task.Delay(PollInterval, stoppingToken);

                try
                {
                    await ProcessAllStacksAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing stacks.");
                }
            }
        }

        private async Task ProcessAllStacksAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var assetNames = await context.Assets.Select(a => a.Name).ToListAsync(ct);

            foreach (var assetName in assetNames)
            {
                var state = await GetOrRestoreStateAsync(context, assetName, ct);

                // Only fetch rows newer than our last checkpoint — never rescan the whole table.
                var newRowsGroupedByTime = await context.SensorRawDatas
                    .Where(r => r.AssetName == assetName &&
                                (state.LastProcessedTimestamp == null || r.TimeStamp > state.LastProcessedTimestamp))
                    .OrderBy(r => r.TimeStamp)
                    .ToListAsync(ct);

                if (newRowsGroupedByTime.Count == 0)
                    continue;

                var byTimestamp = newRowsGroupedByTime
                    .GroupBy(r => r.TimeStamp)
                    .OrderBy(g => g.Key);

                var recordsToInsert = new List<StackEfficiencyRecord>();

                foreach (var tickGroup in byTimestamp)
                {
                    var snapshot = BuildSnapshot(tickGroup, assetName);
                    if (snapshot == null) continue; // incomplete tick (missing a required tag), skip

                    recordsToInsert.Add(_engine.ProcessTick(state, snapshot.Value, DtHours));
                }

                if (recordsToInsert.Count > 0)
                {
                    context.StackEfficiencyRecords.AddRange(recordsToInsert);
                    await context.SaveChangesAsync(ct);
                    _logger.LogInformation("[{Asset}] Processed {Count} new ticks. Efficiency={Eff:F2}%, RemainingLife={Life:F1}h",
                        assetName, recordsToInsert.Count, state.TrackedEfficiency, DegradationConstants.FIXED_EOL_HOURS - state.OperationalHours);
                }
            }
        }

        private RawSignalSnapshot? BuildSnapshot(IGrouping<DateTime, SensorRawData> tickGroup, string assetName)
        {
            double? current = null, voltage = null, temperature = null, pressure = null, h2flow = null;

            foreach (var row in tickGroup)
            {
                switch (row.TagName)
                {
                    case "current": current = row.Value; break;
                    case "voltage": voltage = row.Value; break;
                    case "temperature": temperature = row.Value; break;
                    case "pressure": pressure = row.Value; break;
                    case "h2flow": h2flow = row.Value; break;
                }
            }

            if (current == null || voltage == null || temperature == null || pressure == null || h2flow == null)
                return null; // Python hasn't written all 5 tags for this tick yet — skip, will catch up next cycle

            return new RawSignalSnapshot(current.Value, voltage.Value, temperature.Value, pressure.Value, h2flow.Value, tickGroup.Key);
        }

        private async Task<StackProcessingState> GetOrRestoreStateAsync(ApplicationDbContext context, string assetName, CancellationToken ct)
        {
            if (_stateByAsset.TryGetValue(assetName, out var cached))
                return cached;

            // Restore from the last persisted record so a restart doesn't reset the stack to BOL.
            var lastRecord = await context.StackEfficiencyRecords
                .Where(r => r.AssetName == assetName)
                .OrderByDescending(r => r.TimeStamp)
                .FirstOrDefaultAsync(ct);

            var state = new StackProcessingState { AssetName = assetName };

            if (lastRecord != null)
            {
                state.TrackedEfficiency = lastRecord.TrackedEfficiency;
                state.OperationalHours = lastRecord.OperationalHours;
                state.LastProcessedTimestamp = lastRecord.TimeStamp;
                // Note: the smoothing window (RecentRawEfficiencies) starts empty on restart —
                // it will rebuild to full accuracy after SMOOTHING_WINDOW ticks (~1 minute).
            }

            _stateByAsset[assetName] = state;
            return state;
        }
    }
}
