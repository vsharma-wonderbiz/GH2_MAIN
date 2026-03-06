// Infrastructure/BackgroundServices/KpiBackgroundService.cs
using Application.DTOS;
using Application.Interface;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices
{
    public class KpiBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KpiBackgroundService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public KpiBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<KpiBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KPI Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateWeek();

                    await CalculateAndStoreAllKpis();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while calculating KPIs.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CalculateAndStoreAllKpis()
        {
            using var scope = _scopeFactory.CreateScope();

            var kpiService = scope.ServiceProvider.GetRequiredService<KpiCalulationService>();
            var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepositary>();
            var kpiResultRepository = scope.ServiceProvider.GetRequiredService<IKpiResultRepository>();

            //var today = DateTime.UtcNow.Date;
            //var startTime = today.AddDays(-7);
            var (startTime, endTime) = GetLastCompletedWeekRange();
            //var endTime = today;
            //var startTime = today.AddDays(-7);

            _logger.LogInformation($"Calculating KPIs for range: {startTime} → {endTime}");

            var allKpiTags = await tagRepository.GetAllKpiTags();

            var results = new List<KpiTable>();

            foreach (var kpiTag in allKpiTags)
            {
                try
                {
                    var dto = new KpiRequestDto
                    {
                        tagId = kpiTag.TagId,
                        startTime = startTime,
                        endTime = endTime
                    };

                    var result = await kpiService.CalculateKpi(dto);

                    foreach (var asset in result.Assets)
                    {
                        if (asset.KpiValue == null) continue;

                        var alreadyExists = await kpiResultRepository.IsAlreadyCalculated(
                            result.KpiName, asset.AssetName, startTime, endTime);

                        if (alreadyExists) continue;

                        results.Add(new KpiTable(
                            kpiName: result.KpiName,
                            assetName: asset.AssetName,
                            level: kpiTag.TagType.TagName,
                            kpiValue: asset.KpiValue.Value,
                            weekNumber:1,
                            startTime: startTime,
                            endTime: endTime
                        ));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to calculate KPI: {kpiTag.TagName}");
                }
            }

            if (results.Any())
            {
                await kpiResultRepository.AddRangeAsync(results);
                await kpiResultRepository.SaveChangesAsync();
                _logger.LogInformation($"Saved {results.Count} KPI results.");
            }
        }

        private async Task UpdateWeek()
        {
            using var scope = _scopeFactory.CreateScope();

            var kpiResultRepository = scope.ServiceProvider.GetRequiredService<IKpiResultRepository>();

            var allKpis = await kpiResultRepository.GetAllKpisValues();

            foreach (var kpi in allKpis)
            {
                kpi.IncrementWeek();
            }

            await kpiResultRepository.SaveChangesAsync();

            _logger.LogInformation("Week numbers updated successfully.");
        }


        private (DateTime weekStart, DateTime weekEnd) GetLastCompletedWeekRange()
        {
            var today = DateTime.UtcNow.Date;

            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var currentWeekStart = today.AddDays(-diff);

            var lastWeekStart = currentWeekStart.AddDays(-7);
            var lastWeekEnd = currentWeekStart;

            return (lastWeekStart, lastWeekEnd);
        }
    }
}