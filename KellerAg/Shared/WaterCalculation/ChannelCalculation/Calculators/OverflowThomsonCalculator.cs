using KellerAg.Shared.Entities.Calculations.CalculationModels;
using KellerAg.Shared.Entities.FileFormat;
using KellerAg.Shared.Entities.Units;
using System;
using System.Collections.Generic;

namespace KellerAg.Shared.WaterCalculation.ChannelCalculation.Calculators
{
    public static class OverflowThomsonCalculator
    {
        public static Dictionary<DateTime, double?> Calculate(MeasurementFileFormat measurement, OverflowThomsonChannelCalculationModel calculation, UnitInfo offsetChannelUnitInfo = null)
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
                                calculation.Offset, calculation.Density, calculation.Gravity, calculation.WallHeight, calculation.FormFactor, calculation.FormAngle));
                    }
                    else
                    {
                        dict.Add(dataPoint.Time,
                            CalculateSingleCompensated(dataPoint.Values[hydroChannelIndex], calculation.Offset,
                                calculation.Density, calculation.Gravity, calculation.WallHeight, calculation.FormFactor, calculation.FormAngle));
                    }
                }
            }
            return dict;
        }

        public static double? CalculateSingle(double? hydroValue, double? baroValue, double offset, double density, double gravity, double wallHeightPressure, double formFactor, double formAngle)
        {
            if (hydroValue == null || baroValue == null) return null;
            return CalculateSingleCompensated(hydroValue - baroValue, offset, density, gravity, wallHeightPressure, formFactor, formAngle);
        }

        public static double? CalculateSingleCompensated(double? pressureValue, double offset, double density, double gravity, double wallHeightPressure, double formFactor, double formAngle)
        {
            var waterHeight = HeightOfWaterCalculator.CalculateSingleCompensated(pressureValue, offset, density, gravity);
            var wallHeight = HeightOfWaterCalculator.CalculateSingleCompensated(wallHeightPressure, offset, density, gravity);
            if (!waterHeight.HasValue || !wallHeight.HasValue || waterHeight - wallHeight <= 0)
            {
                return 0;
            }
            return (8.0 / 15) * formFactor * Math.Tan(formAngle / 2) * Math.Sqrt(2 * gravity) * Math.Pow((waterHeight.Value - wallHeight.Value), (5.0 / 2));
        }
    }
}
