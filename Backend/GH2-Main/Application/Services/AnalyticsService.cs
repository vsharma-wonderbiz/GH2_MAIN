using Application.DTOS;
using Application.Interface;

namespace Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _analyticsRepository;
        private readonly IAssetRepository _assetRepo;

        public AnalyticsService(
            IAnalyticsRepository analyticsRepository,
            IAssetRepository assetRepository)
        {
            _analyticsRepository = analyticsRepository;
            _assetRepo = assetRepository;
        }

        public async Task<AnalyticsResponseDto> GetAnalyticsData(AnalyticsRequestDto dto)
        {
            
            var asset = await _assetRepo.GetByNameAsync(dto.AssetName);

            if (asset == null)
                throw new InvalidOperationException("Asset not found.");

            if (dto.StartTime >= dto.EndTime)
                throw new InvalidOperationException("Invalid time range.");

            int bucketMinutes = CalculateBucketMinutes(dto.StartTime, dto.EndTime);

            return await _analyticsRepository.GetAggregatedSensorData(
                dto.AssetName,
                dto.TagName,
                dto.StartTime,
                dto.EndTime,
                bucketMinutes
            );
        }

        private int CalculateBucketMinutes(DateTime startTime, DateTime endTime)
        {
            var totalMinutes = (endTime - startTime).TotalMinutes;
            var totalHours = totalMinutes / 60;
            var totalDays = totalHours / 24;

            if (totalHours <= 6)
                return 0;   // raw

            if (totalHours <= 12)
                return 1;

            if (totalDays <= 1)
                return 1;

            if (totalDays <= 10)
                return 2;

            if (totalDays <= 20)
                return 10;

            if (totalDays <= 30)
                return 15;

            return 30;
        }
    }
}