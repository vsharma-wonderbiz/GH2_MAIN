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
            Console.WriteLine("The is service layer");
            Console.WriteLine($"StartTime (raw): {dto.StartTime}");
            Console.WriteLine($"EndTime (raw): {dto.EndTime}");
            Console.WriteLine($"StartTime Kind: {dto.StartTime.Kind}");
            Console.WriteLine($"EndTime Kind: {dto.EndTime.Kind}");

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

            //if (totalHours <= 1)
            //    return null; // raw (5 sec)
            if (totalHours <= 2)
                return 0;

            if (totalHours <= 6)
                return 1; // 1 min  

            if (totalHours <= 12)
                return 2; // 2 min

            if (totalHours <= 24)
                return 5; // 5 min

            if (totalDays <= 7)
                return 15; // 15 min

            if (totalDays <= 15)
                return 30; // 30 min

            if (totalDays <= 30)
                return 60; // 1 hour

            return 120; // 2 hours
        }   
    }
}