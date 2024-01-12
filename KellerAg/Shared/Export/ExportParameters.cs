using KellerAg.Shared.Entities.FileFormat;
using KellerAg.Shared.Entities.Filetypes;
using KellerAg.Shared.Entities.Units;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KellerAg.Shared.Entities.Channel;

namespace KellerAg.Shared.Export
{
    public class ExportParameters
    {
        public Filetype? FileType { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public IEnumerable<UnitInfo> CustomUnits { get; set; }
        //public char DecimalSymbol { get; set; }
        public CultureInfo NumbersFormatCulture { get; set; }
        public char CsvSeparator { get; set; }
        public IEnumerable<MeasurementFileFormatChannelCalculation> ChannelCalculations { get; set; }
    }

    public interface IExportRequest
    {
        /// <summary>
        /// Measurement that should be exported
        /// </summary>
        MeasurementFileFormat Measurement { get; set; }
        /// <summary>
        /// Channels of the measurement that should be exported
        /// </summary>
        IEnumerable<ChannelInfo> Channels { get; set; }
        /// <summary>
        /// Channel calculations of the measurement that should be exported
        /// </summary>
        IEnumerable<MeasurementFileFormatChannelCalculation> ChannelCalculations { get; set; }
        /// <summary>
        /// Info about the newly created file
        /// </summary>
        IExportFileInfo ExportFileInfo { get; set; }
    }

    public class ExportRequest : IExportRequest
    {
        /// <summary>
        /// New export request with selected channels and calculations
        /// </summary>
        /// <param name="exportFileInfo"></param>
        /// <param name="measurement">Measurement to export</param>
        /// <param name="channels">channels to export</param>
        /// <param name="channelCalculations">calculations to export</param>
        public ExportRequest(IExportFileInfo exportFileInfo, MeasurementFileFormat measurement, IEnumerable<ChannelInfo> channels, IEnumerable<MeasurementFileFormatChannelCalculation> channelCalculations)
        {
            ExportFileInfo = exportFileInfo;
            Measurement = measurement;
            Channels = channels;
            ChannelCalculations = channelCalculations;
        }

        /// <summary>
        /// New export request with selected channels with all calculations
        /// </summary>
        /// <param name="exportFileInfo"></param>
        /// <param name="measurement"></param>
        /// <param name="channels"></param>
        public ExportRequest(IExportFileInfo exportFileInfo, MeasurementFileFormat measurement, IEnumerable<ChannelInfo> channels)
        {
            ExportFileInfo = exportFileInfo;
            Measurement = measurement;
            Channels = channels;
            ChannelCalculations = measurement.Header.ChannelCalculations;
        }

        /// <summary>
        /// New export request with all channels and with selected calculations
        /// </summary>
        /// <param name="exportFileInfo"></param>
        /// <param name="measurement"></param>
        /// <param name="channelCalculations"></param>
        public ExportRequest(IExportFileInfo exportFileInfo, MeasurementFileFormat measurement, IEnumerable<MeasurementFileFormatChannelCalculation> channelCalculations)
        {
            ExportFileInfo = exportFileInfo;
            Measurement = measurement;
            ChannelCalculations = channelCalculations;
            Channels = measurement.Header.MeasurementDefinitionsInBody.Select(ChannelInfo.GetChannelInfo);
        }

        /// <summary>
        /// New export request with all channels and calculation
        /// </summary>
        /// <param name="exportFileInfo"></param>
        /// <param name="measurement"></param>
        public ExportRequest(IExportFileInfo exportFileInfo, MeasurementFileFormat measurement)
        {
            ExportFileInfo = exportFileInfo;
            Measurement = measurement;
            ChannelCalculations = measurement.Header.ChannelCalculations;
            Channels = measurement.Header.MeasurementDefinitionsInBody.Select(ChannelInfo.GetChannelInfo);
        }

        /// <inheritdoc />
        public MeasurementFileFormat Measurement { get; set; }
        /// <inheritdoc />
        public IEnumerable<ChannelInfo> Channels { get; set; }
        /// <inheritdoc />
        public IEnumerable<MeasurementFileFormatChannelCalculation> ChannelCalculations { get; set; }
        /// <inheritdoc />
        public IExportFileInfo ExportFileInfo { get; set; }
    }

    public interface IExportFileInfo
    {
        /// <summary>
        /// Path to directory the file should be stored in
        /// </summary>
        string FilePath { get; set; }

        /// <summary>
        /// Name of the export file without file ending
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// Type of the export file
        /// </summary>
        Filetype FileType { get; set; }

        /// <summary>
        /// FilePath, FileName and FileType combined
        /// </summary>
        string FullFilePath { get; }
    }

    public class ExportFileInfo : IExportFileInfo
    {
        public ExportFileInfo(string filePath, string fileName, Filetype fileType)
        {
            FilePath = filePath;
            FileName = fileName;
            FileType = fileType;
        }

        /// <summary>
        /// Use this constructor if the file will be compressed into an archive and therefore does not need a filepath
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileType"></param>
        public ExportFileInfo(string fileName, Filetype fileType)
        {
            FileName = fileName;
            FileType = fileType;
        }

        /// <inheritdoc />
        public string FilePath { get; set; }
        /// <inheritdoc />
        public string FileName { get; set; }
        /// <inheritdoc />
        public Filetype FileType { get; set; }

        public string FullFilePath => Path.Combine(FilePath ?? string.Empty, FileName + FileExtensionHelper.GetFileExtension(FileType));
    }

    public interface IExportPreferences
    {
        /// <summary>
        /// Custom units, default are base units (More info in class UnitInfo)
        /// </summary>
        IEnumerable<UnitInfo> CustomUnits { get; set; }
        /// <summary>
        /// Format culture for numbers, default is CultureInfo.InvariantCulture
        /// </summary>
        CultureInfo NumbersFormatCulture { get; set; }
        /// <summary>
        /// Decimal places per UnitType, default is 2
        /// </summary>
        Dictionary<UnitType, int> DecimalPlaces { get; set; }
    }

    public class ExportPreferences : IExportPreferences
    {
        public ExportPreferences()
        {
            NumbersFormatCulture = CultureInfo.InvariantCulture;
            DecimalPlaces = new Dictionary<UnitType, int>();
            CustomUnits = UnitInfo.BaseUnits();
        }

        /// <inheritdoc />
        public IEnumerable<UnitInfo> CustomUnits { get; set; }
        /// <inheritdoc />
        public CultureInfo NumbersFormatCulture { get; set; }
        /// <inheritdoc />
        public Dictionary<UnitType, int> DecimalPlaces { get; set; }
    }

    public class CsvExportPreferences : ExportPreferences
    {
        public CsvExportPreferences(char csvSeparator) : base()
        {
            CsvSeparator = csvSeparator;
        }

        /// <summary>
        /// CSV Separator
        /// </summary>
        public char CsvSeparator { get; set; }
    }
}
