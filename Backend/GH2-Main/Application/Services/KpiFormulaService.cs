using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class KpiFormulaService
    {
        // Main entry - pass kpiName and { "current": 4.5, "voltage": 120.3, "h2flow": 0.85 }

        public float? Calculate(string kpiName, Dictionary<string, float> tagValues)
        {
            return kpiName switch
            {
                // Stack KPIs
                "stack_specific_energy" => StackSpecificEnergy(tagValues),
                "pressure_diff" => PressureDiff(tagValues),
                "ratio" => Ratio(tagValues),
                "temperature_kpi" => TemperatureKpi(tagValues),
                "voltage_kpi" => VoltageKpi(tagValues),
                "concentration_kpi" => ConcentrationKpi(tagValues),

                // Plant KPIs
                "specific_energy" => SpecificEnergy(tagValues),
                "throughput" => Throughput(tagValues),
                "specific_water_consumption" => SpecificWaterConsumption(tagValues),
                "inlet_water_conductivity" => InletWaterConductivity(tagValues),

                _ => null
            };
        }

        //Stack formulas 
        private float? StackSpecificEnergy(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("current", out var current)) return null;
            if (!v.TryGetValue("voltage", out var voltage)) return null;
            if (!v.TryGetValue("h2flow", out var h2flow) || h2flow == 0) return null;

            return (current * voltage) / h2flow;
        }

        private float? PressureDiff(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("pressure", out var pressure)) return null;
            if (!v.TryGetValue("outlet_pressure", out var outlet)) return null;

            return pressure - outlet;
        }

        private float? Ratio(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("current", out var current)) return null;
            if (!v.TryGetValue("voltage", out var voltage)) return null;
            if (!v.TryGetValue("h2flow", out var h2flow) || h2flow == 0) return null;

            return (current * voltage) / h2flow;
        }

        private float? TemperatureKpi(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("downstream_temp", out var downstream)) return null;
            if (!v.TryGetValue("recombiner_temp", out var recombiner)) return null;

            return Math.Abs(downstream - recombiner);
        }

        private float? VoltageKpi(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("voltage", out var voltage)) return null;
            return voltage;
        }

        private float? ConcentrationKpi(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("concentration", out var concentration)) return null;
            return concentration;
        }

        //  Plant formulas 
        private float? SpecificEnergy(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("power", out var power)) return null;
            if (!v.TryGetValue("h2flow", out var h2flow) || h2flow == 0) return null;

            return power / h2flow;
        }

        private float? Throughput(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("h2flow", out var h2flow)) return null;
            return h2flow;
        }

        private float? SpecificWaterConsumption(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("water_flow_tot", out var water)) return null;
            if (!v.TryGetValue("h2flow", out var h2flow) || h2flow == 0) return null;

            return water / h2flow;
        }

        private float? InletWaterConductivity(Dictionary<string, float> v)
        {
            if (!v.TryGetValue("water_conductivity", out var conductivity)) return null;
            return conductivity;
        }
    }
}