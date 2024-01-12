using KellerAg.Shared.Entities.Calculations.CalculationModels;
using KellerAg.Shared.Entities.FileFormat;
using System;
using System.Collections.Generic;

namespace KellerAg.Shared.WaterCalculation.ChannelCalculation.Calculators
{
    public static class ForceCalculator
    {
        public static Dictionary<DateTime, double?> Calculate(MeasurementFileFormat measurement, ForceChannelCalculationModel calculation)
        {
            var dict = new Dictionary<DateTime, double?>();

            var hydroChannelIndex = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.HydrostaticPressureChannel.MeasurementDefinitionId);
            var baroChannelIndex = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.BarometricPressureChannel?.MeasurementDefinitionId);
            var compensate = calculation.UseBarometricPressureToCompensate;

            if (hydroChannelIndex < 0 || (compensate && baroChannelIndex < 0)) return dict;

            foreach (Measurements dataPoint in measurement.Body)
            {
                if (compensate)
                {
                    dict.Add(dataPoint.Time,
                        CalculateSingle(dataPoint.Values[hydroChannelIndex], dataPoint.Values[baroChannelIndex],
                            calculation.Offset, calculation.Area));
                }
                else
                {
                    dict.Add(dataPoint.Time,
                        CalculateSingleCompensated(dataPoint.Values[hydroChannelIndex],
                            calculation.Offset, calculation.Area));
                }
            }
            return dict;
        }

        public static double? CalculateSingle(double? hydroValue, double? baroValue, double offset, double area)
        {
            if (hydroValue == null || baroValue == null) return null;
            return CalculateSingleCompensated(hydroValue - baroValue, offset, area);
        }

        public static double? CalculateSingleCompensated(double? pressureValue, double offset, double area)
        {
            return (pressureValue + offset) * area * 100000;
        }
    }
}
