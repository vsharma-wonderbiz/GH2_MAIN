using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class PastWeeksAggregatedData
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PastWeeksAggregatedData> _logger;

        public PastWeeksAggregatedData(
            ApplicationDbContext context,
            ILogger<PastWeeksAggregatedData> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("Weekly Backfill Started");

            if (!await _context.SensorRawDatas.AnyAsync())
            {
                _logger.LogWarning("No raw data found.");
                return;
            }

            var minDate = await _context.SensorRawDatas.MinAsync(x => x.TimeStamp);
            var maxDate = await _context.SensorRawDatas.MaxAsync(x => x.TimeStamp);

            // Get current week start (to exclude running week)
            var today = DateTime.UtcNow.Date;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var currentWeekStart = today.AddDays(-diff);

            var startWeek = GetWeekStart(minDate.Date);

            while (startWeek < currentWeekStart)
            {
                var weekStart = startWeek;
                var weekEnd = weekStart.AddDays(6);

                _logger.LogInformation("Processing Week: {WeekStart} - {WeekEnd}",
                    weekStart, weekEnd);

                var mappings = await _context.Mappings
                    .Include(x => x.Asset)
                    .ToListAsync();

                foreach (var mapping in mappings)
                {
                    bool exists = await _context.WeeklyAvgData
                        .AnyAsync(x =>
                            x.MappingId == mapping.MappingId &&
                            x.WeekStartDate == weekStart);

                    if (exists)
                    {
                        _logger.LogInformation("Week already exists for MappingId {MappingId}",
                            mapping.MappingId);
                        continue;
                    }

                    var weeklyQuery = _context.SensorRawDatas
                        .Where(x =>
                            x.MappingId == mapping.MappingId &&
                            x.TimeStamp >= weekStart &&
                            x.TimeStamp <= weekEnd);

                    if (!await weeklyQuery.AnyAsync())
                        continue;

                    var avg = await weeklyQuery.AverageAsync(x => x.Value);
                    var min = await weeklyQuery.MinAsync(x => x.Value);
                    var max = await weeklyQuery.MaxAsync(x => x.Value);
                    var count = await weeklyQuery.CountAsync();

                    var entity = new WeeklyAggregatedData(
                        mapping.AssetId,
                        mapping.MappingId,
                        weekStart,
                        weekEnd,
                        avg,
                        min,
                        max,
                        count,
                        7,
                        true 
                    );

                    await _context.WeeklyAvgData.AddAsync(entity);

                    _logger.LogInformation(
                        "Inserted weekly aggregate for MappingId {MappingId}",
                        mapping.MappingId);
                }

                await _context.SaveChangesAsync();

                startWeek = startWeek.AddDays(7);
            }

            _logger.LogInformation("Weekly Backfill Completed");
        }

        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}