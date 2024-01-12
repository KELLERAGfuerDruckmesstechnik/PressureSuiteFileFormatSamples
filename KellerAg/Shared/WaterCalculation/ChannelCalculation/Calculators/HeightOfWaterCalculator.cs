using KellerAg.Shared.Entities.Calculations.CalculationModels;
using KellerAg.Shared.Entities.FileFormat;
using System;
using System.Linq;
using System.Collections.Generic;

namespace KellerAg.Shared.WaterCalculation.ChannelCalculation.Calculators
{
    public static class HeightOfWaterCalculator
    {
        public static Dictionary<DateTime, double?> Calculate(MeasurementFileFormat measurement, HeightOfWaterChannelCalculationModel calculation)
        {
            var dict = new Dictionary<DateTime, double?>();

            var hydroChannelIndex = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.HydrostaticPressureChannel.MeasurementDefinitionId);
            var baroChannelIndex  = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.BarometricPressureChannel?.MeasurementDefinitionId);
            var compensate = calculation.UseBarometricPressureToCompensate;

            if (hydroChannelIndex < 0 || (compensate && baroChannelIndex < 0)) return dict;

            // measurement is after 'from date' and before 'to date'
            var measurementsInRange = measurement.Body.Where(x => (calculation.FromDate == null || x.Time.CompareTo(calculation.FromDate) >= 0) && (calculation.ToDate == null || x.Time.CompareTo(calculation.ToDate) <= 0));

            foreach (Measurements dataPoint in measurementsInRange)
            {
                if (compensate)
                {
                    dict.Add(dataPoint.Time,
                        CalculateSingle(dataPoint.Values[hydroChannelIndex], dataPoint.Values[baroChannelIndex],
                            calculation.Offset, calculation.Density, calculation.Gravity));
                }
                else
                {
                    dict.Add(dataPoint.Time,
                        CalculateSingleCompensated(dataPoint.Values[hydroChannelIndex],
                            calculation.Offset, calculation.Density, calculation.Gravity));
                }
            }
            return dict;
        }

        public static double? CalculateSingle(double? hydroValue, double? baroValue, double offset, double density, double gravity)
        {
            if (hydroValue == null || baroValue == null) return null;
            return CalculateSingleCompensated(hydroValue - baroValue, offset, density, gravity);
        }

        public static double? CalculateSingleCompensated(double? pressureValue, double offset, double density, double gravity)
        {
            return ((pressureValue * 100000) / (density * gravity)) + offset;
        }
        public static double? RetrievePressure(double height, double offset, double density, double gravity)
        {
            return ((height - offset) * (density * gravity)) / 100000;
        }
    }
}
