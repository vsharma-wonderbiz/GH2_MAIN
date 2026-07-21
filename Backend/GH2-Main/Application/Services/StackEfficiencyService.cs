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
            //Actual (downsampled) data DB se lao
            var data = await _efficiencyRepository.GetDownSampledRecords(assetname, POINTS_PER_SEGMENT);

            if (data == null || data.Count == 0)
            {
                return new EfficiencyGraphDto
                {
                    Actual = new List<EfficiencyRecordDto>(),
                    Predicted = new List<EfficiencyRecordDto>()
                };
            }

           
            var actualPoints = data.Select(a => new EfficiencyRecordDto
            {
                OperationalHour = a.OperationalHours,
                Efficiency = a.TrackedEfficiency,
            }).ToList();

            // Last record uthao - yahi se prediction start hogi
            var lastRecord = data[^1];
            var lastOperationalHour = lastRecord.OperationalHours;
            var lastTrackedEfficiency = lastRecord.TrackedEfficiency;

            var forecastPoints = _forecastCalculator.PredictFuture(
                lastOperationalHour,
                lastTrackedEfficiency,
                POINTS_PER_SEGMENT);

            // Forecast points ko bhi same DTO shape mein convert karo
            var predictedPoints = forecastPoints.Select(f => new EfficiencyRecordDto
            {
                OperationalHour = f.OperationalHours,
                Efficiency = f.PredictedEfficiency,
            }).ToList();

            //Dono ko combine karke return karo
            return new EfficiencyGraphDto
            {
                Actual = actualPoints,
                Predicted = predictedPoints
            };
        }


        public async Task<EfficiencyResult> GetLatestEfficiencyAsync(string stackName)
        {
            var record = await _efficiencyRepository.GetLatestEfficiency(stackName);

            if (record == null)
                return new EfficiencyResult { StackName = stackName, Status = "No data" };

           
            var efficiency = Math.Round(record.TrackedEfficiency, 2);
            var status = efficiency < 70 ? "Critical" : "Normal";

            return new EfficiencyResult
            {
                StackName = stackName,
                LatestEfficiency = efficiency,
                OperationalHours = record.OperationalHours,
                Status = status
            };
        }
    }
}