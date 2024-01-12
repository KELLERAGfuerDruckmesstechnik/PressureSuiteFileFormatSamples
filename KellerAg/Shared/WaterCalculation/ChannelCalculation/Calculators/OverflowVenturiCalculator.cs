using KellerAg.Shared.Entities.Calculations.CalculationModels;
using KellerAg.Shared.Entities.FileFormat;
using KellerAg.Shared.Entities.Units;
using System;
using System.Collections.Generic;

namespace KellerAg.Shared.WaterCalculation.ChannelCalculation.Calculators
{
    public static class OverflowVenturiCalculator
    {
        public static Dictionary<DateTime, double?> Calculate(MeasurementFileFormat measurement, OverflowVenturiChannelCalculationModel calculation, UnitInfo offsetChannelUnitInfo = null)
        {
            var dict = new Dictionary<DateTime, double?>();

            var hydroChannelIndex = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.HydrostaticPressureChannel.MeasurementDefinitionId);
            var baroChannelIndex = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.BarometricPressureChannel?.MeasurementDefinitionId);
            var compensate = calculation.UseBarometricPressureToCompensate;

            if (hydroChannelIndex >= 0 && (!compensate || baroChannelIndex >= 0))
            {
                foreach (var dataPoint in measurement.Body)
                {
                    if (compensate)
                    {
                        dict.Add(dataPoint.Time,
                            CalculateSingle(dataPoint.Values[hydroChannelIndex], dataPoint.Values[baroChannelIndex],
                                calculation.Offset, calculation.Density, calculation.Gravity, calculation.FormFactor, calculation.FormWidth));
                    }
                    else
                    {
                        dict.Add(dataPoint.Time,
                            CalculateSingleCompensated(dataPoint.Values[hydroChannelIndex], calculation.Offset,
                                calculation.Density, calculation.Gravity, calculation.FormFactor, calculation.FormWidth));
                    }
                }
            }
            return dict;
        }

        public static double? CalculateSingle(double? hydroValue, double? baroValue, double offset, double density, double gravity, double formFactor, double formWidth)
        {
            if (hydroValue == null || baroValue == null) return null;
            return CalculateSingleCompensated(hydroValue - baroValue, offset, density, gravity, formFactor, formWidth);
        }

        public static double? CalculateSingleCompensated(double? pressureValue, double offset, double density, double gravity, double formFactor, double formWidth)
        {
            var waterHeight = HeightOfWaterCalculator.CalculateSingleCompensated(pressureValue, offset, density, gravity);
            if (!waterHeight.HasValue)
            {
                return 0;
            }
            return formFactor * formWidth * Math.Pow(waterHeight.Value, (3.0 / 2));
        }
    }
}
