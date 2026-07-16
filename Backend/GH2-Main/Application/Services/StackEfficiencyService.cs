using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;
using Domain.Services;

namespace Application.Services
{
    public class StackEfficiencyService : IStackEfficiencyService
    {
        private readonly IStackEfficiencyRepository _efficiencyRepository;
        private readonly EfficiencyForecastCalculator _forecastCalculator;

        private const int POINTS_PER_SEGMENT = 100;

        public StackEfficiencyService(
            IStackEfficiencyRepository efficiencyRepository,
            EfficiencyForecastCalculator forecastCalculator)
        {
            _efficiencyRepository = efficiencyRepository;
            _forecastCalculator = forecastCalculator;
        }

        public async Task<EfficiencyGraphDto> GetEfficiencyData(string assetname)
        {
            // Step 1: Actual (downsampled) data DB se lao
            var data = await _efficiencyRepository.GetDownSampledRecords(assetname, POINTS_PER_SEGMENT);

            if (data == null || data.Count == 0)
            {
                return new EfficiencyGraphDto
                {
                    Actual = new List<EfficiencyRecordDto>(),
                    Predicted = new List<EfficiencyRecordDto>()
                };
            }

            // Step 2: Actual data ko DTO mein convert karo
            var actualPoints = data.Select(a => new EfficiencyRecordDto
            {
                OperationalHour = a.OperationalHours,
                Efficiency = a.TrackedEfficiency,
            }).ToList();

            // Step 3: Last record uthao - yahi se prediction start hogi
            var lastRecord = data[^1];
            var lastOperationalHour = lastRecord.OperationalHours;
            var lastTrackedEfficiency = lastRecord.TrackedEfficiency;

            // Step 4: Forecast calculator ko call karo
            var forecastPoints = _forecastCalculator.PredictFuture(
                lastOperationalHour,
                lastTrackedEfficiency,
                POINTS_PER_SEGMENT);

            // Step 5: Forecast points ko bhi same DTO shape mein convert karo
            var predictedPoints = forecastPoints.Select(f => new EfficiencyRecordDto
            {
                OperationalHour = f.OperationalHours,
                Efficiency = f.PredictedEfficiency,
            }).ToList();

            // Step 6: Dono ko combine karke return karo
            return new EfficiencyGraphDto
            {
                Actual = actualPoints,
                Predicted = predictedPoints
            };
        }
    }
}