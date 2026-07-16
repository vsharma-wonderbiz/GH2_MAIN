using System;
using System.Collections.Generic;

namespace Domain.Services
{
    public class EfficiencyForecastCalculator
    {
        public record ForecastPoint(double OperationalHours, double PredictedEfficiency);

        private const double FIXED_END_HOURS = 40000.0;
        private const double FIXED_END_EFFICIENCY = 60.0;

        public List<ForecastPoint> PredictFuture(
            double lastActualHour,
            double lastActualEfficiency,
            int numberOfFuturePoints = 100)
        {

            double startHour = lastActualHour;
            double startEfficiency = lastActualEfficiency;

            if (numberOfFuturePoints <= 0)
                return new List<ForecastPoint>();

            if (startHour >= FIXED_END_HOURS || startEfficiency <= FIXED_END_EFFICIENCY)
                return new List<ForecastPoint>();

            double totalHourGap = FIXED_END_HOURS - startHour;
            double totalEfficiencyGap = FIXED_END_EFFICIENCY - startEfficiency;
            double slope = totalEfficiencyGap / totalHourGap;

            var forecast = new List<ForecastPoint>();
            double step = totalHourGap / numberOfFuturePoints;

            for (int i = 1; i <= numberOfFuturePoints; i++)
            {
                double futureHour = startHour + (step * i);
                double predictedEfficiency = startEfficiency + slope * (futureHour - startHour);
                forecast.Add(new ForecastPoint(futureHour, predictedEfficiency));
            }

            if (forecast.Count > 0)
            {
                forecast[^1] = new ForecastPoint(FIXED_END_HOURS, FIXED_END_EFFICIENCY);
            }

            return forecast;
        }
    }
}