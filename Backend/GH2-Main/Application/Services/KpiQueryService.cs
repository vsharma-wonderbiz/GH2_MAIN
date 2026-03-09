// Application/Services/KpiQueryService.cs
using Application.DTOS;
using Application.Interface;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class KpiQueryService
    {
        private readonly IKpiResultRepository _kpiResultRepository;
        private readonly KpiCalulationService _kpiCalulationService;
        private readonly ITagRepositary _tagRepositary;
        private readonly ILogger<KpiQueryService> _logger;
        public KpiQueryService(
            IKpiResultRepository kpiResultRepository,
            KpiCalulationService kpiCalulationService,
            ITagRepositary tagRepositary,
            ILogger<KpiQueryService> logger)
        {
            _kpiResultRepository = kpiResultRepository;
            _kpiCalulationService = kpiCalulationService;
            _tagRepositary = tagRepositary;
            _logger = logger;
        }

        public async Task<KpiQueryResultDto> GetKpiAsync(KpiQueryRequestDto request)
        {
            var (startTime, endTime) = ResolveTimeRange(request);

            // For last-week and custome requests, try cache first
            if (request.TimeRange == KpiTimeRange.LastWeek  || request.TimeRange==KpiTimeRange.Custom)
            {
                var cached = await TryGetFromCache(request.TagId, startTime, endTime);
                if (cached != null)
                {
                    _logger.LogInformation("Serving KPI from cache for TagId={TagId}", request.TagId);
                    return cached;
                }
                else
                {
                    _logger.LogInformation("No kpi data in cahce moing to live calculation");
                }
            }

            // Live calculation for last-hour, last-24h, custom, or cache miss
            _logger.LogInformation(
                "Calculating KPI live for TagId={TagId}, Range={Start} → {End}",
                request.TagId, startTime, endTime);

            var liveResult = await _kpiCalulationService.CalculateKpi(new KpiRequestDto
            {
                tagId = request.TagId,
                startTime = startTime,
                endTime = endTime
            });

            return new KpiQueryResultDto
            {
                KpiName = liveResult.KpiName,
                StartTime = startTime,
                EndTime = endTime,
                Source = KpiDataSource.LiveCalculation,
                Assets = liveResult.Assets.Select(a => new KpiAssetResultDto
                {
                    AssetName = a.AssetName,
                    KpiValue = a.KpiValue,
                    //Mappings = a.Mappings
                }).ToList()
            };
        }

        // ── Private Helpers ──────────────────────────────────────────────────────

        private (DateTime start, DateTime end) ResolveTimeRange(KpiQueryRequestDto request)
        {
            var now = DateTime.UtcNow;

            return request.TimeRange switch
            {
                KpiTimeRange.LastHour => (now.AddHours(-1), now),
                KpiTimeRange.Last24Hours => (now.AddHours(-24), now),
                KpiTimeRange.LastWeek => GetLastCompletedWeekRange(),
                KpiTimeRange.Custom => (
                    request.CustomStart ?? throw new ArgumentException("CustomStart required"),
                    request.CustomEnd ?? throw new ArgumentException("CustomEnd required")),
                _ => throw new ArgumentOutOfRangeException(nameof(request.TimeRange))
            };
        }

        private async Task<KpiQueryResultDto?> TryGetFromCache(
            int tagId, DateTime startTime, DateTime endTime)
        {
            var tag = await _tagRepositary.GetTagNameById(tagId);
            var kpiName = tag.TagName;

            var cached = await _kpiResultRepository
                .GetByKpiNameAndDateRange(kpiName, startTime, endTime);

            if (cached == null || !cached.Any())
                return null;

            return new KpiQueryResultDto
            {
                KpiName = kpiName,
                StartTime = startTime,
                EndTime = endTime,
                Source = KpiDataSource.Cache,
                Assets = cached.Select(c => new KpiAssetResultDto
                {
                    AssetName = c.AssetName,
                    KpiValue = c.KpiValue,
                    StartTime=c.StartTime,
                    EndTime=c.EndTime
                    //Mappings = new List<TagMappingDto>() // cache doesn't store raw mappings
                }).ToList()
            };
        }

        private (DateTime weekStart, DateTime weekEnd) GetLastCompletedWeekRange()
        {
            var today = DateTime.UtcNow.Date;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var currentWeekStart = today.AddDays(-diff);
            return (currentWeekStart.AddDays(-7), currentWeekStart);
        }
    }
}