﻿using KellerAg.Shared.Entities.FileFormat;
using KellerAg.Shared.Entities.Calculations;
using System;
using System.Linq;
using System.Collections.Generic;
using KellerAg.Shared.Entities.Calculations.CalculationModels;

namespace KellerAg.Shared.WaterCalculation.ChannelCalculation.Calculators
{
    public static class DepthToWaterCalculator
    {

        public static Dictionary<DateTime, double?> Calculate(MeasurementFileFormat measurement, DepthToWaterChannelCalculationModel calculation)
        {
            var dict = new Dictionary<DateTime, double?>();

            var hydroChannelIndex = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.HydrostaticPressureChannel.MeasurementDefinitionId);
            var baroChannelIndex  = Array.IndexOf(measurement.Header.MeasurementDefinitionsInBody, calculation.BarometricPressureChannel?.MeasurementDefinitionId);
            var compensate = calculation.UseBarometricPressureToCompensate;

            // measurement is after 'from date' and before 'to date'
            var measurementsInRange = measurement.Body.Where(x => (calculation.FromDate == null || x.Time.CompareTo(calculation.FromDate) >= 0) && (calculation.ToDate == null || x.Time.CompareTo(calculation.ToDate) <= 0));

            if (hydroChannelIndex >= 0 && (!compensate || baroChannelIndex >= 0))
            {
                foreach (var dataPoint in measurementsInRange)
                {
                    if (compensate)
                    {
                        dict.Add(dataPoint.Time,
                            CalculateSingle(dataPoint.Values[hydroChannelIndex], dataPoint.Values[baroChannelIndex],
                                calculation.Offset, calculation.Density, calculation.Gravity, calculation.InstallationLength));
                    }
                    else
                    {
                        dict.Add(dataPoint.Time,
                            CalculateSingleCompensated(dataPoint.Values[hydroChannelIndex],
                                calculation.Offset, calculation.Density, calculation.Gravity, calculation.InstallationLength));
                    }
                }
            }
            return dict;
        }

        public static double? CalculateSingle(double? hydroValue, double? baroValue, double offset, double density, double gravity, double installationLength)
        {
            if (hydroValue == null || baroValue == null) return null;
            return CalculateSingleCompensated(hydroValue - baroValue, offset, density, gravity, installationLength);
        }

        public static double? CalculateSingleCompensated(double? pressureValue, double offset, double density, double gravity, double installationLength)
        {
            return installationLength - ((pressureValue * 100000) / (density * gravity)) + offset;
        }
    }
}
