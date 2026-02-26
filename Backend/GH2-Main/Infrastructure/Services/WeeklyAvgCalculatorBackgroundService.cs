using Application.Interface;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class WeeklyAvgCalculatorBackgroundService : BackgroundService
{
    //private readonly IAnalyticsRepository analyticsRepository;
    //private readonly ApplicationDbContext context;
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

                    var (weekStart, weekEnd) = GetLastCompletedWeekRange();
                   

                    _logger.LogInformation("Processing weekly aggregation for {WeekStart} - {WeekEnd}",
                        weekStart, weekEnd);

                    var mappings = await context.Mappings
                                  .Where(a => a.Tag.TagTypeId != 3)
                                 .Include(a => a.Asset)
                                 .Include(b => b.Tag)
                                 .ToListAsync();

                    _logger.LogInformation($" these is all the mapping {mappings.ToString()}");
                    foreach (var mapping in mappings)
                    {
                        Console.WriteLine($"MappingId: {mapping.MappingId}, AssetId: {mapping.AssetId}, TagId: {mapping.TagId}, OpcNodeId: {mapping.OpcNodeId}");
                    }

                    foreach (var mapping in mappings)
                    {
                        bool exists = await analyticsRepository
                            .IsWeekAvgDataPresent(mapping.MappingId, weekStart,weekEnd);
                        Console.Write(exists);


                        if (exists)
                        {
                            _logger.LogInformation(
                                "Weekly data already exists for MappingId {MappingId}",
                                mapping.MappingId);
                            continue;
                        }

                        var aggregate = await analyticsRepository
                            .GetWeeklyAggregateAsync(mapping.MappingId, weekStart, weekEnd);

                        if (aggregate == null || aggregate.Count == 0)
                        {
                            _logger.LogWarning(
                                "No raw data found for MappingId {MappingId}",
                                mapping.MappingId);
                            continue;
                        }

                        var datapoint = new WeeklyAggregatedData(
                            mapping.Asset.AssetId,
                            mapping.MappingId,
                            weekStart,
                            weekEnd,
                            aggregate.Average,
                            aggregate.Min,
                            aggregate.Max,
                            aggregate.Count
                        );

                        await analyticsRepository.AddAsync(datapoint);

                        _logger.LogInformation(
                            "Inserted weekly aggregate for MappingId {MappingId}",
                            mapping.MappingId);
                    }

                    await analyticsRepository.SaveChangesAsync();
                }
            
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during weekly aggregation.");
            }

            // Run once per day (safe interval)
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Weekly Average Background Service stopped.");
    }

    private (DateTime weekStart, DateTime weekEnd) GetLastCompletedWeekRange()
    {
        var today = DateTime.UtcNow.Date;

        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var currentWeekStart = today.AddDays(-1 * diff);

        var lastWeekStart = currentWeekStart.AddDays(-7);
        var lastWeekEnd = currentWeekStart.AddTicks(-1);

        return (lastWeekStart, lastWeekEnd);
    }
}