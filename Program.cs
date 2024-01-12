using KellerAg.Shared.Entities.FileFormat;
using Newtonsoft.Json;
using KellerAg.Shared.Entities.Channel;
using KellerAg.Shared.Entities.Filetypes;
using KellerAg.Shared.Export;
using KellerAg.Shared.Export.ExportEngines;

namespace ReadKellerIoTMeasurementFile;

internal class Program
{
    static void Main(string[] args)
    {
        var jsonText = File.ReadAllText("ExampleData/EUI-F84F25000001A0AD_2024-01-12_01_25_17.json");

        MeasurementFileFormat measurementFile = JsonConvert.DeserializeObject<MeasurementFileFormat>(jsonText);

        Console.WriteLine($"The loaded measurement file has {measurementFile.Body.Count} measurements with {measurementFile.Header.MeasurementDefinitionsInBody.Length} channels and {measurementFile.Header.ChannelCalculations.Count} calculated channel/s.");

        // These are the use with methods of KELLER to create CSV data using the KellerAg.Shared.x libraries:
        List<ChannelInfo> channelInfos = measurementFile.Header.MeasurementDefinitionsInBody.Select(ChannelInfo.GetChannelInfo).ToList();
        var exportFileInfo = new ExportFileInfo("", "filename.csv", Filetype.Csv);
        var request = new ExportRequest(exportFileInfo, measurementFile, channelInfos, measurementFile.Header.ChannelCalculations);
        object[,] table = ExportHelper.CreateDataTable(request, new CsvExportPreferences(',') , true);
        
        // Print first 50 rows CSV style
        for (int row = 0; row < 50 && row < table.GetLength(0); row++)
        {
            for (int column = 0; column < table.GetLength(1); column++)
            {
                Console.Write(table[row, column] + ",");
            }
            Console.WriteLine();
        }

        // You do not have to use these methods. Implement your own. 
        // The process is:
        // 1. Read the measurement file
        // 2. Tabulate the measurement data from "Body" into a table
        // 3. If necessary, change the time from UTC to another (local) time zone
        // 4. Use a lookup table to translate the integer ids from "MeasurementDefinitionsInBody" into the correct channel names
        // 5. If necessary, calculate values for the calculated channels in "ChannelCalculations" and add them to the table

        Console.ReadLine();
    }
}