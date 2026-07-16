using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Services;
using Domain.Entities;

namespace Domain.Services
{

    public class StackProcessingState
    {
        public required string AssetName { get; set; }
        public double TrackedEfficiency { get; set; } = DegradationConstants.BOL_EFFICIENCY;
        public double OperationalHours { get; set; } = 0.0;
        public DateTime? LastProcessedTimestamp { get; set; } = null;

        // Rolling window for the moving-average smoothing step (PDF Step 2).
        // ASSUMPTION: window size N wasn't specified in the PDF — defaulted to 12
        // samples (= 1 minute at a 5-second tick). Adjust EfficiencyCalculationEngine.SMOOTHING_WINDOW if needed.
        public Queue<double> RecentRawEfficiencies { get; } = new Queue<double>();
    }

    public readonly record struct RawSignalSnapshot(
        double Current, double Voltage, double Temperature, double Pressure, double H2FlowRaw, DateTime Timestamp);
    public class EfficiencyCalculationEngine
    {

            // ASSUMPTION: not specified in PDF, see note on StackProcessingState.RecentRawEfficiencies
            public const int SMOOTHING_WINDOW = 12;

            public double ClampStress(double raw) => Math.Max(0.0, Math.Min(1.0, raw));

            public double ComputeStressT(double temp) =>
                ClampStress((temp - DegradationConstants.TEMP_STRESS_LOW) /
                            (DegradationConstants.TEMP_STRESS_HIGH - DegradationConstants.TEMP_STRESS_LOW));

            public double ComputeStressI(double current) =>
                ClampStress((current - DegradationConstants.CURRENT_STRESS_LOW) /
                            (DegradationConstants.CURRENT_STRESS_HIGH - DegradationConstants.CURRENT_STRESS_LOW));

            public double ComputeStressP(double pressure) =>
                ClampStress((pressure - DegradationConstants.PRESSURE_STRESS_LOW) /
                            (DegradationConstants.PRESSURE_STRESS_HIGH - DegradationConstants.PRESSURE_STRESS_LOW));

            public double ComputeStressV(double voltage) =>
                ClampStress(Math.Abs(voltage - DegradationConstants.VOLTAGE_NOMINAL) /
                            DegradationConstants.VOLTAGE_ALLOWED_RANGE);

            public double ComputeMuEffective(double stressT, double stressI, double stressP, double stressV)
            {
                double baseRate = DegradationConstants.BASE_DEGRADATION_RATE_PERCENT_PER_HOUR;
                double mu = baseRate * (1
                    + DegradationConstants.W_TEMP * stressT
                    + DegradationConstants.W_CURRENT * stressI
                    + DegradationConstants.W_PRESSURE * stressP
                    + DegradationConstants.W_VOLTAGE * stressV);
                return Math.Max(mu, baseRate); // PDF Step 4 floor
            }

            /// <summary>
            /// Runs one tick of raw signals through the full PDF Steps 1-7 pipeline,
            /// mutating the given state in place and returning the record to persist.
            /// This is THE single source of truth — called identically whether the
            /// tick came from the live Python-fed data or historical backfilled data.
            /// </summary>
            public StackEfficiencyRecord ProcessTick(StackProcessingState state, RawSignalSnapshot signal, double dtHours)
            {
                // --- Step 1 & 2: Faraday theoretical vs actual, raw efficiency ---
                double h2TheoreticalNm3h = DegradationConstants.FARADAY_CONSTANT_K * signal.Current * DegradationConstants.N_CELLS;
                double h2ActualNm3h = signal.H2FlowRaw / 1000.0; // NL/h -> Nm3/h

                double rawEfficiency = h2TheoreticalNm3h > 0
                    ? (h2ActualNm3h / h2TheoreticalNm3h) * 100.0
                    : 0.0;

                // --- Smoothing (moving average of last N raw samples) ---
                state.RecentRawEfficiencies.Enqueue(rawEfficiency);
                while (state.RecentRawEfficiencies.Count > SMOOTHING_WINDOW)
                    state.RecentRawEfficiencies.Dequeue();
                double smoothedEfficiency = state.RecentRawEfficiencies.Average();

                // --- Step 3: stress factors ---
                double stressT = ComputeStressT(signal.Temperature);
                double stressI = ComputeStressI(signal.Current);
                double stressP = ComputeStressP(signal.Pressure);
                double stressV = ComputeStressV(signal.Voltage);

                // --- Step 4: base -> effective degradation rate ---
                double mu = ComputeMuEffective(stressT, stressI, stressP, stressV);

                // --- Step 5: compare SMOOTHED MEASURED efficiency against the ideal
                // reference line, and accelerate mu if reality is already worse ---
                double expectedEfficiency = DegradationConstants.BOL_EFFICIENCY
                    - (20.0 * state.OperationalHours / DegradationConstants.FIXED_EOL_HOURS);
                double deviation = expectedEfficiency - smoothedEfficiency;
                if (deviation > 0)
                {
                    mu *= (1 + DegradationConstants.DEVIATION_CORRECTION_K * deviation);
                }

                // --- Step 7: update the OFFICIAL tracked efficiency (this is what
                // the chart/UI actually uses — not the raw or smoothed measured value) ---
                double newTrackedEfficiency = state.TrackedEfficiency - (mu * dtHours);
                newTrackedEfficiency = Math.Max(
                    DegradationConstants.EOL_EFFICIENCY,
                    Math.Min(DegradationConstants.BOL_EFFICIENCY, newTrackedEfficiency));

                state.TrackedEfficiency = newTrackedEfficiency;
                state.OperationalHours += dtHours;
                state.LastProcessedTimestamp = signal.Timestamp;

                return new StackEfficiencyRecord
                {
                    AssetName = state.AssetName,
                    TimeStamp = signal.Timestamp,
                    Current = signal.Current,
                    Voltage = signal.Voltage,
                    Temperature = signal.Temperature,
                    Pressure = signal.Pressure,
                    H2FlowRaw = signal.H2FlowRaw,
                    RawMeasuredEfficiency = Math.Round(rawEfficiency, 4),
                    SmoothedMeasuredEfficiency = Math.Round(smoothedEfficiency, 4),
                    TrackedEfficiency = Math.Round(newTrackedEfficiency, 4),
                    ExpectedEfficiency = Math.Round(expectedEfficiency, 4),
                    Deviation = Math.Round(deviation, 4),
                    MuEffective = mu,
                    OperationalHours = Math.Round(state.OperationalHours, 4),
                    RemainingLifeHours = Math.Round(DegradationConstants.FIXED_EOL_HOURS - state.OperationalHours, 4)
                };
            }
        }
    }

