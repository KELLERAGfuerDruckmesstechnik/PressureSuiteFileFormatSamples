using KellerAg.Shared.Entities.FileFormat;
using KellerAg.Shared.Entities.Units;
using System;
using System.Collections.Generic;
using KellerAg.Shared.Entities.Calculations.CalculationModels;

namespace KellerAg.Shared.WaterCalculation.ChannelCalculation.Calculators
{
    public static class OffsetCalculator
    {
        public static Dictionary<DateTime, double?> Calculate(MeasurementFileFormat measurement, OffsetChannelCalculationModel calculation, UnitInfo offsetChannelUnitInfo = null)
        {
            var dict = new Dictionary<DateTime, double?>();

            var channelIndex = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.OffsetChannel.MeasurementDefinitionId);

            if (channelIndex < 0) return dict; // not found in MeasurementDefinitionsInBody

            foreach (var dataPoint in measurement.Body)
            {
                dict.Add(dataPoint.Time, CalculateSingle(dataPoint.Values[channelIndex], calculation.Offset, offsetChannelUnitInfo));
            }

            return dict;
        }

        public static double? CalculateSingle(double? sourceValue, double offset, UnitInfo offsetChannelUnitInfo = null)
        {
            if (offsetChannelUnitInfo != null && sourceValue.HasValue)
            {
                return offsetChannelUnitInfo.ToBase(offsetChannelUnitInfo.FromBase(sourceValue.Value) + offset);
            }
            else if (sourceValue.HasValue)
            {
                return sourceValue + offset;
            }

            return null;
        }
    }
}
