using KellerAg.Shared.Entities.Calculations.CalculationModels;
using KellerAg.Shared.Entities.FileFormat;
using KellerAg.Shared.Entities.Units;
using System;
using System.Collections.Generic;

namespace KellerAg.Shared.WaterCalculation.ChannelCalculation.Calculators
{
    public static class TankCalculator
    {
        public static Dictionary<DateTime, double?> Calculate(MeasurementFileFormat measurement, TankChannelCalculationModel calculation, UnitInfo offsetChannelUnitInfo = null)
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
                            CalculateSingle(dataPoint.Values[hydroChannelIndex], dataPoint.Values[baroChannelIndex], calculation.TankTypeId, 
                            calculation.Density, calculation.Gravity, calculation.Height, calculation.Width, calculation.Length, calculation.InstallationLength));
                    }
                    else
                    {
                        dict.Add(dataPoint.Time,
                            CalculateSingleCompensated(dataPoint.Values[hydroChannelIndex], calculation.TankTypeId,
                            calculation.Density, calculation.Gravity, calculation.Height, calculation.Width, calculation.Length, calculation.InstallationLength));
                    }
                }
            }
            return dict;
        }

        public static double? CalculateSingle(double? hydroValue, double? baroValue, int tankTypeId, double density, double gravity, double height, double width, double length, double installationHeight)
        {
            if (hydroValue == null || baroValue == null) return null;
            return CalculateSingleCompensated(hydroValue - baroValue, tankTypeId, density, gravity, height, width, length, installationHeight);
        }

        public static double? CalculateSingleCompensated(double? pressureValue, int tankTypeId, double density, double gravity, double height, double width, double length, double installationHeight)
        {
            var heightOfWaterNullable = HeightOfWaterCalculator.CalculateSingleCompensated(pressureValue, height - installationHeight, density, gravity);
            if (!heightOfWaterNullable.HasValue)
            {
                return 0;
            }
            var heightOfWater = heightOfWaterNullable.Value;

            switch (tankTypeId)
            {
                case 1: // CylinderVertical

                    var volume = heightOfWater * (width / 2) * (length / 2) * Math.PI;
                    return volume * 1000;

                case 2: // CylinderHorizontal

                    var paramA = Math.Acos(1 - 2 * heightOfWater / height);
                    var paramB = (1 - 2 * heightOfWater / height);
                    var paramC = Math.Sqrt(4 * heightOfWater / height - 4 * Math.Pow(heightOfWater, 2) / Math.Pow(height, 2));
                    var A = height * width / 4 * (paramA - paramB * paramC);
                    var V = A * length;
                    return V * 1000;

                case 3: // Cube

                    var cubeVolume = heightOfWater * width * length;
                    return cubeVolume * 1000;

                case 4: // KloepperBoden

                    return CalculateKloepperBoden(heightOfWater, height, width);

                case 5: // Sphere

                    var SphereVolume = Math.Pow(heightOfWater, 2) * Math.PI / 3 * (3 * (height / 2) - heightOfWater);
                    return SphereVolume * 1000;
            }
            return 0;
        }

        private static double CalculateKloepperBoden(double heightOfWater, double diameter, double length)
        {
            // Thickness of wall. Could be used in the future
            double s = 0;
            // Small radius. Standard defines 1/10 of big radius
            double radiusB = diameter / 10;
            // Length of cylindrical tube
            double L = Math.Sqrt(65 * Math.Pow(radiusB, 2) + 8 * radiusB * s - Math.Pow(s, 2));
            // Length of Klöpperboden
            double ha = diameter - L;
            // Length of spherical cap
            double hb = ha - L / 9;
            // Length of torus region
            double hc = L / 9;
            // Length of cylindrical region
            double hd = length - 2 * ha;
            // Height of torus region
            double he = radiusB - Math.Sqrt(Math.Pow(radiusB, 2) - Math.Pow(hc, 2));
            // Half of inner radius
            double hf = diameter / 2 - s;
            // Is more than one half filled?
            bool q = heightOfWater > (diameter / 2 - s);
            // Height for volume calculation
            double h1 = (!q) ? heightOfWater : (heightOfWater > (2 * hf)) ? 0 : 2 * hf - heightOfWater;
            // Height in torus region
            double h2 = (h1 >= he) ? he : h1;
            // Length in torus region
            double h3 = Math.Sqrt(2 * h2 * radiusB - Math.Pow(h2, 2));
            // Height of the spherical cap
            double h4 = (h1 <= he) ? 0 : h1 - he;

            // Volume of Cylinder
            double Va = Math.Pow(hf, 2) * Math.PI * hd;
            // Volume of torus region
            double Vb = (-1 / 3 * Math.Pow(hc, 3) + (Math.Pow(radiusB, 2) + Math.Pow(4 * radiusB - s, 2)) * hc + (4 * radiusB - s) * (hc *
                Math.Sqrt(Math.Pow(radiusB, 2) - Math.Pow(hc, 2)) +
                Math.Pow(radiusB, 2) * Math.Asin(hc / radiusB))) * Math.PI;

            // Volume of spherical cap
            double Vc = Math.Pow(hb, 2) * (diameter - hb / 3) * Math.PI;
            // Tank volume
            double Vd = Va + 2 * (Vb + Vc);

            double param1V1;
            if ((hf - he - h4) >= Math.Sqrt(Math.Pow(diameter, 2) - Math.Pow(diameter - hb, 2)))
            {
                param1V1 = Math.PI / 6 * Math.Pow(hb, 2) * ((diameter - hb) + 2 * diameter);
            }
            else
            {
                param1V1 = 2.0 / 3.0 * Math.Pow(diameter, 3) *
                    Math.Atan(((hf - he - h4) * (diameter - hb)) / (diameter *
                            Math.Sqrt(
                                Math.Pow(diameter, 2) - Math.Pow(hf - he - h4, 2) - Math.Pow(diameter - hb, 2)
                            ))
                    ) + (diameter - hb) / 3 * (Math.Pow(diameter - hb, 2) - 3 * Math.Pow(diameter, 2)) *
                    Math.Atan((hf - he - h4) /
                        Math.Sqrt(
                            Math.Pow(diameter, 2) - Math.Pow(hf - he - h4, 2) - Math.Pow(diameter - hb, 2)
                        )
                    ) - (hf - he - h4) / 3 * (Math.Pow(hf - he - h4, 2) - 3 * Math.Pow(diameter, 2)) *
                    Math.Acos((diameter - hb) /
                        Math.Sqrt(
                            Math.Pow(diameter, 2) - Math.Pow(hf - he - h4, 2)
                        )
                    ) - 2.0 / 3.0 * (hf - he - h4) * (diameter - hb) *
                    Math.Sqrt(Math.Pow(diameter, 2) - Math.Pow(hf - he - h4, 2) - Math.Pow(diameter - hb, 2));
            }

            // Volume in the spherical caps
            double V1 = Math.PI / 3 * Math.Pow(hb, 2) * ((diameter - hb) + 2 * diameter) - 2 * param1V1;
            // Volume in the cylinder
            double V2 = (Math.Pow(hf, 2) * Math.Acos((hf - h1) / hf) - (hf - h1) * Math.Sqrt(Math.Pow(hf, 2) - Math.Pow(hf - h1, 2))) * hd;

            double param1T1 = (Math.Abs(h2) < 0.000001) ? 0 : h3 / h2 * hf;
            double param2T1 = (Math.Abs(hf - h1) < 0.000001) ? 0 : Math.Log(hf - h1);
            // Volume of the spherical segment
            double T1 = param1T1 / (6 * hf) * (Math.PI * Math.Pow(hf, 3) - 4 * (hf - h1) * hf *
                Math.Sqrt(Math.Pow(hf, 2) - Math.Pow(hf - h1, 2)) - 2 * Math.Pow(hf, 3) *
                Math.Asin((hf - h1) / hf) + 2 * Math.Pow(hf - h1, 3) * (
                    Math.Log(hf + Math.Sqrt(Math.Pow(hf, 2) - Math.Pow(hf - h1, 2))) - param2T1));

            // Volume of the spherical segment outside of the torus
            double T2 = 0;
            if (h1 > he)
            {
                double param1T2 = (Math.Abs(h2) < 0.000001) ? 0 : h3 / h2 * hf - hc;
                double param2T2 = ((hf - h1) <= 0) ? 0 : Math.Log(hf - h1);

                T2 = param1T2 / (6 * (hf - he)) * (Math.PI * Math.Pow(hf - he, 3) - 4 * (hf - h1) * (hf - he) *
                    Math.Sqrt(Math.Pow(hf - he, 2) - Math.Pow(hf - h1, 2)) - 2 * Math.Pow(hf - he, 3) *
                    Math.Asin((hf - h1) / (hf - he)) + 2 * Math.Pow(hf - h1, 3) *
                    (Math.Log((hf - he) + Math.Sqrt(Math.Pow(hf - he, 2) - Math.Pow(hf - h1, 2))) - param2T2));
            }

            // Volume of spherical segment for torus region
            double V3 = 2 * (T1 - T2);
            // Rotation angle
            double alpha = ((Math.Abs(h1) < 0.000001) ? 0 : Math.Acos(
                (hf - h1) / ((radiusB - h2 / 2) * (0.6 *
                Math.Sqrt(radiusB * (radiusB - h2 / 2)) + 0.4 * radiusB) /
                Math.Sqrt(radiusB * (radiusB - h2 / 2)) + hf - radiusB)) * 2)
                * 180 / Math.PI;

            double param1T3 = ((Math.Pow(radiusB, 2) - Math.Pow(h3, 2)) < 0 || Math.Abs(radiusB) < 0.000001) ? 0 :
                h3 * Math.Sqrt(Math.Pow(radiusB, 2) - Math.Pow(h3, 2)) + Math.Pow(radiusB, 2) * Math.Asin(h3 / radiusB);

            double T3 = (-1 / 3 * Math.Pow(h3, 3) + (Math.Pow(radiusB, 2) +
                Math.Pow(hf - radiusB, 2)) * h3 + (hf - radiusB) * param1T3) * alpha / 360 * Math.PI;

            double T4 = Math.Pow((Math.Abs(h3) < 0.000001) ? 0 : -1 * h2 / h3, 2);
            double param1T4 = (Math.Abs(h3) < 0.000001) ? 0 : -1 * h2 / h3;
            T4 = T4 / 3 * Math.Pow(h3, 3) + param1T4 * hf * Math.Pow(h3, 2) + Math.Pow(hf, 2) * h3;
            T4 *= alpha / 360 * Math.PI;

            // approximated rest-volume
            double V4 = 2 * (T3 - T4);

            // Volume in cubic meter
            double V = (q) ? (Vd - V1 - V2 - V3 - V4) : (V1 + V2 + V3 + V4);

            return V * 1000;
        }
    }
}
