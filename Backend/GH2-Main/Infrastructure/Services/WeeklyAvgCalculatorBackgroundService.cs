using Application.Interface;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class WeeklyAvgCalculatorBackgroundService : BackgroundService
{
    private readonly ILogger<WeeklyAvgCalculatorBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public WeeklyAvgCalculatorBackgroundService(
       IServiceScopeFactory scopeFactory,
       ILogger<WeeklyAvgCalculatorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Weekly Average Background Service will start after 10 minutes delay...");

        // 🔥 Startup Delay Here
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

        _logger.LogInformation("Weekly Average Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var analyticsRepository =
                        scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>();

                    var context =
                        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var (weekStart, weekEnd) = GetCurrentWeekRange();

                    _logger.LogInformation("Processing rolling weekly aggregation for {WeekStart} - {WeekEnd}",
                        weekStart, weekEnd);

                    var mappings = await context.Mappings
                                  .Where(a => a.Tag.IsDerived==false)
                                  .Include(a => a.Asset)
                                  .Include(b => b.Tag)
                                  .ToListAsync();

                    foreach (var mapping in mappings)
                    {
                        var dailyAggregate = await analyticsRepository
                            .GetWeeklyAggregateAsync(mapping.MappingId, weekStart, DateTime.UtcNow);

                        if (dailyAggregate == null || dailyAggregate.Count == 0)
                        {
                            _logger.LogWarning(
                                "No raw data found for MappingId {MappingId}",
                                mapping.MappingId);
                            continue;
                        }

                        bool IsExsist = await analyticsRepository
                            .IsWeekAvgDataPresent(mapping.MappingId, weekStart, weekEnd);

                        if (IsExsist)
                        {
                            var enitiy = await analyticsRepository
                                .GetByAssetMappingAndWeekAsync(mapping.AssetId, mapping.MappingId, weekStart);

                            var newAvg = (enitiy.AverageValue * enitiy.TotalSamples + dailyAggregate.Average * dailyAggregate.Count)
                                          / (enitiy.TotalSamples + dailyAggregate.Count);

                            var newMin = Math.Min(enitiy.MinValue, dailyAggregate.Min);
                            var newMax = Math.Max(enitiy.MaxValue, dailyAggregate.Max);
                            var newTotalSamples = enitiy.TotalSamples + dailyAggregate.Count;
                            var newWeekEnd = DateTime.UtcNow;

                            enitiy.UpdateAggregates(newAvg, newMin, newMax, newTotalSamples, newWeekEnd);

                            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                                enitiy.UpdateIsFinale(true);

                            await analyticsRepository.UpdateAsync(enitiy);

                            _logger.LogInformation(
                                "Updated rolling weekly aggregate for MappingId {MappingId}",
                                mapping.MappingId);
                        }
                        else
                        {
                            var datapoint = new WeeklyAggregatedData(
                                mapping.Asset.AssetId,
                                mapping.MappingId,
                                weekStart,
                                weekEnd,
                                dailyAggregate.Average,
                                dailyAggregate.Min,
                                dailyAggregate.Max,
                                dailyAggregate.Count,
                                1,
                                DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday
                            );

                            await analyticsRepository.AddAsync(datapoint);

                            _logger.LogInformation(
                                "Inserted rolling weekly aggregate for MappingId {MappingId}",
                                mapping.MappingId);
                        }
                    }

                    await analyticsRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during weekly aggregation.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        _logger.LogInformation("Weekly Average Background Service stopped.");
    }

    // Current week range: Monday -> Sunday
    private (DateTime weekStart, DateTime weekEnd) GetCurrentWeekRange()
    {
        var today = DateTime.UtcNow.Date;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var weekStart = today.AddDays(-diff);
        var weekEnd = weekStart.AddDays(6);
        return (weekStart, weekEnd);
    }
}