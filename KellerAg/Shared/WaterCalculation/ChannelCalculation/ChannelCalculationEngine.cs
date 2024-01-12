using KellerAg.Shared.Entities.Calculations;
using KellerAg.Shared.Entities.Calculations.CalculationModels;
using KellerAg.Shared.Entities.FileFormat;
using KellerAg.Shared.Entities.Units;
using KellerAg.Shared.WaterCalculation.ChannelCalculation.Calculators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KellerAg.Shared.WaterCalculation.ChannelCalculation
{
    public static class ChannelCalculationEngine
    {
        public static Dictionary<DateTime, double?> CalculateChannel(MeasurementFileFormat measurement, MeasurementFileFormatChannelCalculation calculation, UnitInfo[] currentUnitInfos)
        {
            //filter out duplicates
            measurement.Body = measurement.Body.Distinct(new DistinctTimeInMeasurementsComparer()).OrderBy(_ => _.Time).ToList();

            switch (CalculationTypeInfo.GetCalculationType(calculation.CalculationTypeId))
            {
                case CalculationType.HeightOfWater: //1
                    var heightOfWaterCalc = new HeightOfWaterChannelCalculationModel(calculation);
                    return HeightOfWaterCalculator.Calculate(measurement, heightOfWaterCalc);
                case CalculationType.DepthToWater: //2
                    var depthToWaterCalc = new DepthToWaterChannelCalculationModel(calculation);
                    return DepthToWaterCalculator.Calculate(measurement, depthToWaterCalc);
                case CalculationType.HeightOfWaterAboveSea: //3
                    var heightAboveSeaCalc = new HeightOfWaterAboveSeaChannelCalculationModel(calculation);
                    return HeightOfWaterAboveSeaCalculator.Calculate(measurement, heightAboveSeaCalc);
                case CalculationType.Offset:
                    var offsetCalc = new OffsetChannelCalculationModel(calculation);
                    return OffsetCalculator.Calculate(measurement, offsetCalc, currentUnitInfos.FirstOrDefault(x => x.UnitType == offsetCalc.ChannelInfo.UnitType));
                case CalculationType.OverflowPoleni:
                    var poleniCalc = new OverflowPoleniChannelCalculationModel(calculation);
                    return OverflowPoleniCalculator.Calculate(measurement, poleniCalc);
                case CalculationType.OverflowThomson:
                    var thomsonCalc = new OverflowThomsonChannelCalculationModel(calculation);
                    return OverflowThomsonCalculator.Calculate(measurement, thomsonCalc);
                case CalculationType.OverflowVenturi:
                    var venturiCalc = new OverflowVenturiChannelCalculationModel(calculation);
                    return OverflowVenturiCalculator.Calculate(measurement, venturiCalc);
                case CalculationType.Force:
                    var forceCalc = new ForceChannelCalculationModel(calculation);
                    return ForceCalculator.Calculate(measurement, forceCalc);
                case CalculationType.Tank:
                    var tankCalc = new TankChannelCalculationModel(calculation);
                    return TankCalculator.Calculate(measurement, tankCalc);
                case CalculationType.Unknown:
                    break;
            }
            return null;
        }

        /// <summary>
        /// For measurements.Body from the cloud there might be nulls in the double values.
        /// </summary>
        /// <param name="measurement"></param>
        /// <param name="currentUnitInfos"></param>
        /// <returns></returns>
        public static Dictionary<ChannelCalculationModelBase, Dictionary<DateTime, double?>> CalculateChannels(MeasurementFileFormat measurement, UnitInfo[] currentUnitInfos)
        {
            if (measurement?.Header?.ChannelCalculations == null)
            {
                return null;
            }
            //filter out duplicates
            measurement.Body = measurement.Body.Distinct(new DistinctTimeInMeasurementsComparer()).OrderBy(_ => _.Time).ToList();

            var dictionary = new Dictionary<ChannelCalculationModelBase, Dictionary<DateTime, double?>>();

            foreach (MeasurementFileFormatChannelCalculation calculation in measurement.Header.ChannelCalculations)
            {
                switch (CalculationTypeInfo.GetCalculationType(calculation.CalculationTypeId))
                {
                    case CalculationType.HeightOfWater: //1
                        var heightOfWaterCalc = new HeightOfWaterChannelCalculationModel(calculation);
                        dictionary.Add(heightOfWaterCalc, HeightOfWaterCalculator.Calculate(measurement, heightOfWaterCalc));
                        break;
                    case CalculationType.DepthToWater: //2
                        var depthToWaterCalc = new DepthToWaterChannelCalculationModel(calculation);
                        dictionary.Add(depthToWaterCalc, DepthToWaterCalculator.Calculate(measurement, depthToWaterCalc));
                        break;
                    case CalculationType.HeightOfWaterAboveSea: //3
                        var heightAboveSeaCalc = new HeightOfWaterAboveSeaChannelCalculationModel(calculation);
                        dictionary.Add(heightAboveSeaCalc, HeightOfWaterAboveSeaCalculator.Calculate(measurement, heightAboveSeaCalc));
                        break;
                    case CalculationType.Offset:
                        var offsetCalc = new OffsetChannelCalculationModel(calculation);
                        dictionary.Add(offsetCalc, OffsetCalculator.Calculate(measurement, offsetCalc, currentUnitInfos.FirstOrDefault(x => x.UnitType == offsetCalc.ChannelInfo.UnitType)));
                        break;
                    case CalculationType.OverflowPoleni:
                        var poleniCalc = new OverflowPoleniChannelCalculationModel(calculation);
                        dictionary.Add(poleniCalc, OverflowPoleniCalculator.Calculate(measurement, poleniCalc));
                        break;
                    case CalculationType.OverflowThomson:
                        var thomsonCalc = new OverflowThomsonChannelCalculationModel(calculation);
                        dictionary.Add(thomsonCalc, OverflowThomsonCalculator.Calculate(measurement, thomsonCalc));
                        break;
                    case CalculationType.OverflowVenturi:
                        var venturiCalc = new OverflowVenturiChannelCalculationModel(calculation);
                        dictionary.Add(venturiCalc, OverflowVenturiCalculator.Calculate(measurement, venturiCalc));
                        break;
                    case CalculationType.Force:
                        var forceCalc = new ForceChannelCalculationModel(calculation);
                        dictionary.Add(forceCalc, ForceCalculator.Calculate(measurement, forceCalc));
                        break;
                    case CalculationType.Tank:
                        var tankCalc = new TankChannelCalculationModel(calculation);
                        dictionary.Add(tankCalc, TankCalculator.Calculate(measurement, tankCalc));
                        break;
                }
            }

            return dictionary;
        }

    }
    public class DistinctTimeInMeasurementsComparer : IEqualityComparer<Measurements>
    {
        public bool Equals(Measurements x, Measurements y)
        {
            return x.Time == y.Time;
        }
        public int GetHashCode(Measurements obj)
        {
            return obj.Time.GetHashCode();
        }
    }
}
