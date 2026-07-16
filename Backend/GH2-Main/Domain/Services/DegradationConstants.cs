using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Services
{
    public class DegradationConstants
    {
        // --- Given directly in the PDF ---
        public const double BOL_EFFICIENCY = 80.0;                          // %
        public const double EOL_EFFICIENCY = 60.0;                          // %
        public const double FIXED_EOL_HOURS = 40000.0;                     // h
        public const double BASE_DEGRADATION_RATE_PERCENT_PER_HOUR = 0.5 / 1000.0; // 0.0005 %/h
        public const double FARADAY_CONSTANT_K = 0.000418;                 // Nm3/h per Amp, PER CELL

        // --- Derived earlier, not yet datasheet-confirmed ---
        public const int N_CELLS = 28;

        // --- Stress thresholds, given in the PDF ---
        public const double TEMP_STRESS_LOW = 48, TEMP_STRESS_HIGH = 55;
        public const double CURRENT_STRESS_LOW = 41, CURRENT_STRESS_HIGH = 43;
        public const double PRESSURE_STRESS_LOW = 25, PRESSURE_STRESS_HIGH = 30;

        // --- ASSUMPTION: PDF gives the Stress_V formula but not exact numbers ---
        public const double VOLTAGE_NOMINAL = 44.05;
        public const double VOLTAGE_ALLOWED_RANGE = 0.05;

        // --- Stress weights, given in the PDF (sum to 1.0) ---
        public const double W_TEMP = 0.2, W_CURRENT = 0.4, W_PRESSURE = 0.2, W_VOLTAGE = 0.2;

        // --- ASSUMPTION: PDF's Step 5 uses "k" but never defines its value ---
        public const double DEVIATION_CORRECTION_K = 0.05;
    }
}
