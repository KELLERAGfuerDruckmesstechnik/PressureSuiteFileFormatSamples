using KellerAg.Shared.Entities.Channel;
using KellerAg.Shared.Entities.Localization;
using KellerAg.Shared.Entities.FileFormat;
using KellerAg.Shared.Entities.Units;
using KellerAg.Shared.WaterCalculation.ChannelCalculation;

namespace KellerAg.Shared.Export.ExportEngines
{
    public static class ExportHelper
    {
        /// <summary> 
        /// The datetime will be localized here
        /// </summary>
        /// <param name="request"></param>
        /// <param name="preferences"></param>
        /// <param name="isSimpleHeaderNeeded"></param>
        /// <returns></returns>
        public static object[,] CreateDataTable(IExportRequest request, IExportPreferences preferences, bool isSimpleHeaderNeeded)
        {
            var allSeries = new List<Dictionary<DateTime, double?>>(/*at least*/request.Measurement.Body.Count);
            List<IGrouping<DateTime, Measurements>> groupedBody = request.Measurement.Body.GroupBy(set => set.Time).ToList();

            //Create series for all non-calculated channels
            foreach (var channel in request.Channels)
            {
                //if (!request.Measurement.Header.MeasurementDefinitionsInBody.Contains(channel.MeasurementDefinitionId))
                //{
                //    throw new ExportFailedException($"Export Failed - Needed Data for MeasurementDefinitionId {channel.MeasurementDefinitionId} is not in Body." +
                //                                    $" MeasurementDefinitionIdsToExport: {string.Join(", ", request.Channels.Select(ch => ch.MeasurementDefinitionId).ToArray())}" +
                //                                    $"MeasurementDefinitionsInBody: {string.Join(", ", request.Measurement.Header.MeasurementDefinitionsInBody?.ToArray())}");
                //}

                Dictionary<DateTime, double?> sortedSeries;
                int posInBody = Array.IndexOf(request.Measurement.Header.MeasurementDefinitionsInBody, channel.MeasurementDefinitionId);

                var channelUnitType = channel.UnitType;
                if (preferences != null && preferences.CustomUnits.Any(unit => unit?.UnitType == channelUnitType))
                {
                    var channelUnit = preferences.CustomUnits.First(unit => unit?.UnitType == channelUnitType);
                    sortedSeries = groupedBody.ToDictionary(measurementSet => measurementSet.Key, measurementSet => channelUnit.FromBase(measurementSet.First().Values[posInBody]));
                }
                else
                {
                    sortedSeries = groupedBody.ToDictionary(measurementSet => measurementSet.Key, measurementSet => measurementSet.First().Values[posInBody]);
                }

                allSeries.Add(sortedSeries);
            }

            //Create series for all calculated channels
            if (request.ChannelCalculations != null)
            {
                foreach (var calculation in request.ChannelCalculations)
                {
                    Dictionary<DateTime, double?> calculatedSeries = ChannelCalculationEngine.CalculateChannel(request.Measurement, calculation, preferences?.CustomUnits?.ToArray());
                    if(calculatedSeries == null)
                    {
                        allSeries.Add(new Dictionary<DateTime, double?>());
                        continue;
                    }

                    if (preferences.CustomUnits.Any(unit => unit?.UnitType == calculation.ChannelInfo.UnitType))
                    {
                        var channelUnit = preferences.CustomUnits.First(unit => unit?.UnitType == calculation.ChannelInfo.UnitType);
                        calculatedSeries = calculatedSeries.ToDictionary(measurementSet => measurementSet.Key, measurementSet => channelUnit.FromBase(measurementSet.Value));
                    }

                    var calculatedAndSortedSeries = new Dictionary<DateTime, double?>(calculatedSeries);
                    allSeries.Add(calculatedAndSortedSeries);
                }
            }

            var dth = new DateTimeHelper(request.Measurement.Header.IanaTimeZoneName);

            //merge together to a sorted list
            var sortedTableList = new SortedList<DateTime, double?[]>();
            for (var idIndex = 0; idIndex < allSeries.Count; idIndex++)
            {
                Dictionary<DateTime, double?> series = allSeries[idIndex];
                foreach (KeyValuePair<DateTime, double?> kvp in series)
                {
                    DateTime dt = kvp.Key;
                    if (!sortedTableList.ContainsKey(dt))
                    {
                        sortedTableList.Add(dt, new double?[allSeries.Count]);
                    }
                    sortedTableList[dt][idIndex] = kvp.Value;
                }
            }

            int measurementsCount = sortedTableList.Count;

            const int columnIdWhereMeasurementsStarts = 3; // Data from the measurements starts at column 3
            var table = new object[measurementsCount + 1, allSeries.Count + columnIdWhereMeasurementsStarts];

            if (isSimpleHeaderNeeded)
            {
                AddSimpleColumnNamesToTable(request, table, columnIdWhereMeasurementsStarts, preferences.CustomUnits);
            }
            else
            {
                AddColumnNamesToTable(request, table, columnIdWhereMeasurementsStarts, preferences.CustomUnits);
            }

            //Add the iteration number for each measurement
            for (var i = 1; i <= measurementsCount; i++)
            {
                table[i, 0] = i;
            }

            // Fill values with Datetime into table
            int y = 1;
            int x = columnIdWhereMeasurementsStarts;
            foreach (KeyValuePair<DateTime, double?[]> kvp in sortedTableList)
            {
                table[y, 1] = dth.LocalizeDateTime(kvp.Key);
                table[y, 2] = kvp.Key; //utc
                for (var i = 0; i < kvp.Value.Length; i++)
                {
                    table[y, x + i] = kvp.Value[i];
                }
                y++;
            }

            return table;
        }

        private static void AddColumnNamesToTable(IExportRequest request,
            object[,] table,
            int columnIdWhereMeasurementsStarts,
            IEnumerable<UnitInfo> customUnits
        )
        {
            table[0, 0] = "No";
            table[0, 1] = "Datetime " + Environment.NewLine + "[local time]" + Environment.NewLine + request.Measurement.Header.IanaTimeZoneName;
            table[0, 2] = "Datetime " + Environment.NewLine + "[UTC]";

            List<ChannelInfo> allChannelsToExport = new List<ChannelInfo>(request.Channels.Count());
            allChannelsToExport.AddRange(request.Channels);

            // Add Channel names and units
            foreach (var channel in allChannelsToExport)
            {
                string channelName = channel.Name;
                UnitType unitType = channel.UnitType;
                UnitInfo[] allUnitFromType = UnitInfo.GetUnits(unitType);
                var unitText = "";
                if (customUnits.Any(unit => unit?.UnitType == unitType))
                {
                    unitText = customUnits.First(unit => unit?.UnitType == unitType).ShortName;
                }
                else if (allUnitFromType.Any())
                {
                    //The first is the one we support like m, bar, °C,..
                    unitText = allUnitFromType.First().ShortName;
                }

                table[0, columnIdWhereMeasurementsStarts] = $"{channelName} {Environment.NewLine}[{unitText}]";

                columnIdWhereMeasurementsStarts++;
            }

            // Add Calculation names and units
            if (request.ChannelCalculations != null)
            {
                foreach (var calc in request.ChannelCalculations)
                {
                    string channelName = calc.ChannelInfo.Name;
                    UnitType unitType = calc.ChannelInfo.UnitType;
                    UnitInfo[] allUnitFromType = UnitInfo.GetUnits(unitType);
                    var unitText = "";
                    if (customUnits.Any(unit => unit?.UnitType == unitType))
                    {
                        unitText = customUnits.First(unit => unit?.UnitType == unitType).ShortName;
                    }
                    else if (allUnitFromType.Any())
                    {
                        //The first is the one we support like m, bar, °C,..
                        unitText = allUnitFromType.First().ShortName;
                    }

                    table[0, columnIdWhereMeasurementsStarts] = $"{channelName} {Environment.NewLine}[{unitText}]";

                    columnIdWhereMeasurementsStarts++;
                }
            }
        }

        private static void AddSimpleColumnNamesToTable(IExportRequest request,
            object[,] table,
            int columnIdWhereMeasurementsStarts,
            IEnumerable<UnitInfo> customUnits
        )
        {
            table[0, 0] = "No";
            table[0, 1] = "Datetime [local time]" + request.Measurement.Header.IanaTimeZoneName;
            table[0, 2] = "Datetime [UTC]";

            List<ChannelInfo> allChannelsToExport = new List<ChannelInfo>(request.Channels.Count());
            allChannelsToExport.AddRange(request.Channels);

            foreach (var channel in allChannelsToExport)
            {
                string channelName = channel.Name;
                UnitType unitType = channel.UnitType;
                UnitInfo[] allUnitFromType = UnitInfo.GetUnits(unitType);
                var unitText = "";

                if (customUnits.Any(unit => unit?.UnitType == unitType))
                {
                    unitText = customUnits.First(unit => unit?.UnitType == unitType).ShortName;
                }
                else if (allUnitFromType.Any())
                {
                    //The first is the one we support like m, bar, °C,..
                    unitText = allUnitFromType.First().ShortName;
                }

                table[0, columnIdWhereMeasurementsStarts] = $"{channelName} [{unitText}] - {request.Measurement.Header.DeviceName}";

                columnIdWhereMeasurementsStarts++;
            }
            // Add Calculation names and units
            if (request.ChannelCalculations != null)
            {
                foreach (var calc in request.ChannelCalculations)
                {
                    string channelName = calc.ChannelInfo.Name;
                    UnitType unitType = calc.ChannelInfo.UnitType;
                    UnitInfo[] allUnitFromType = UnitInfo.GetUnits(unitType);
                    var unitText = "";
                    if (customUnits.Any(unit => unit?.UnitType == unitType))
                    {
                        unitText = customUnits.First(unit => unit?.UnitType == unitType).ShortName;
                    }
                    else if (allUnitFromType.Any())
                    {
                        //The first is the one we support like m, bar, °C,..
                        unitText = allUnitFromType.First().ShortName;
                    }

                    table[0, columnIdWhereMeasurementsStarts] = $"{channelName} {Environment.NewLine}[{unitText}]";

                    columnIdWhereMeasurementsStarts++;
                }
            }
        }


        //internal static void CreateComment(IWorksheet sheet, string sheetCell, string text)
        //{
        //    ICommentShape commentShape1 = sheet[sheetCell].AddComment();
        //    commentShape1.AutoSize = true;
        //    commentShape1.IsVisible = false;
        //    commentShape1.Text = text;
        //}
    }
}
