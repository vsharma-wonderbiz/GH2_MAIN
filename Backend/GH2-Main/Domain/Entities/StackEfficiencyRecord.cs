using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class StackEfficiencyRecord
    {
        public int Id { get; set; }
        public required string AssetName { get; set; }
        public DateTime TimeStamp { get; set; }

        // Raw signals used for this tick (kept for auditing/debugging)
        public double Current { get; set; }
        public double Voltage { get; set; }
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public double H2FlowRaw { get; set; }

        // The three distinct efficiency values (see explanation above)
        public double RawMeasuredEfficiency { get; set; }      // Faraday ratio, this tick only, noisy
        public double SmoothedMeasuredEfficiency { get; set; } // moving avg of raw, last N samples
        public double TrackedEfficiency { get; set; }          // the OFFICIAL running health value (chart/UI use this)

        public double ExpectedEfficiency { get; set; }         // reference line, for audit
        public double Deviation { get; set; }                  // Expected - Smoothed, for audit
        public double MuEffective { get; set; }                // degradation speed applied this tick

        public double OperationalHours { get; set; }
        public double RemainingLifeHours { get; set; }
    }
}
