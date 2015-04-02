// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VoltageAccumulationWorkflow.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the VoltageAccumulationWorkflow type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows.VoltageAccumulation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.IO;
    using ImsInformed.Workflows.CrossSectionExtraction;

    using UIMFLibrary;

    /// <summary>
    /// Essentially just accumulate voltages of a UIMF file and split it.
    /// </summary>
    public class VoltageAccumulationWorkflow
    {
        /// <summary>
        /// The number of frames.
        /// </summary>
        public readonly double NumberOfFrames;

        /// <summary>
        /// The number of scans.
        /// </summary>
        public readonly int NumberOfBins;

        private readonly bool averageNotSum;

        private readonly string inputPath;

        private string datasetName;

        /// <summary>
        /// The UIMF reader.
        /// </summary>
        public readonly DataReader UimfReader;

        public VoltageAccumulationWorkflow(bool averageNotSum, string uimfLocation, string outputDirectory)
        {
            this.UimfReader = new DataReader(uimfLocation);
            this.inputPath = uimfLocation;
            GlobalParams globalParams = this.UimfReader.GetGlobalParams();
            this.NumberOfFrames = globalParams.GetValueInt32(GlobalParamKeyType.NumFrames);
            this.averageNotSum = averageNotSum;
            
            this.datasetName = Path.GetFileNameWithoutExtension(uimfLocation);
            if (outputDirectory == string.Empty)
            {
                outputDirectory = Directory.GetCurrentDirectory();
            } 
            
            if (!outputDirectory.EndsWith("\\"))
            {
                outputDirectory += "\\";
            }

            if (!Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to create directory.");
                    throw;
                }
            }

            this.OutputDir = outputDirectory;
            GlobalParams globalParam = this.UimfReader.GetGlobalParams();
            this.NumberOfFrames = globalParam.NumFrames;
            this.NumberOfBins = globalParam.GetValueInt32(GlobalParamKeyType.Bins);
        }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputDir { get; set; }

        /// <summary>
        /// The run voltage accumulation workflow.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool RunVoltageAccumulationWorkflow(FileFormatEnum exportFormat)
        {
            return this.RunVoltageAccumulationWorkflow(0, 0, true, exportFormat);
        }

        /// <summary>
        /// The run voltage accumulation workflow.
        /// </summary>
        /// <param name="startScan">
        /// The start scan.
        /// </param>
        /// <param name="endScan">
        /// The end scan.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool RunVoltageAccumulationWorkflow(int startScan, int endScan, FileFormatEnum exportFormat)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, false, exportFormat);
        }

        /// <summary>
        /// The run voltage accumulation workflow.
        /// </summary>
        /// <param name="startScan">
        /// The start scan.
        /// </param>
        /// <param name="endScan">
        /// The end scan.
        /// </param>
        /// <param name="startBin">
        /// The start bin.
        /// </param>
        /// <param name="endBin">
        /// The end bin.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool RunVoltageAccumulationWorkflow(int startScan, int endScan, int startBin, int endBin, FileFormatEnum exportFormat)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, startBin, endBin, false, exportFormat);
        }

        /// <summary>
        /// The run voltage accumulation workflow.
        /// </summary>
        /// <param name="startScan">
        /// The start scan.
        /// </param>
        /// <param name="endScan">
        /// The end scan.
        /// </param>
        /// <param name="startBin">
        /// The start bin.
        /// </param>
        /// <param name="endBin">
        /// The end bin.
        /// </param>
        /// <param name="xCompression">
        /// The x compression.
        /// </param>
        /// <param name="yCompression">
        /// The y compression.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool RunVoltageAccumulationWorkflow(int startScan, int endScan, int startBin, int endBin, double xCompression, double yCompression, FileFormatEnum exportFormat)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, startBin, endBin, xCompression, yCompression, false, exportFormat);
        }

        /// <summary>
        /// The run voltage accumulation workflow.
        /// </summary>
        /// <param name="startScan">
        /// The start scan.
        /// </param>
        /// <param name="endScan">
        /// The end scan.
        /// </param>
        /// <param name="fullScan">
        /// The full scan.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool RunVoltageAccumulationWorkflow(int startScan, int endScan, bool fullScan, FileFormatEnum exportFormat)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, 1, this.NumberOfBins, fullScan, exportFormat);
        }

        /// <summary>
        /// The run voltage accumulation workflow.
        /// </summary>
        /// <param name="startScan">
        /// The start scan.
        /// </param>
        /// <param name="endScan">
        /// The end scan.
        /// </param>
        /// <param name="startBin">
        /// The start bin.
        /// </param>
        /// <param name="endBin">
        /// The end bin.
        /// </param>
        /// <param name="fullScan">
        /// The full scan.
        /// </param>
        /// <param name="exportFormat"></param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool RunVoltageAccumulationWorkflow(int startScan, int endScan, int startBin, int endBin, bool fullScan, FileFormatEnum exportFormat)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, startBin, endBin, -1, -1, fullScan, exportFormat);
        }

        /// <summary>
        /// The input UIMF file needs to be a UIMF file created by direct-injection
        /// IMS, with different drift tube voltages at different frames.
        /// This constructor intelligently group voltages together by observing
        /// sharp changes in running voltage standard deviation. The entire mobility
        /// and frame range would be accumulated.
        /// </summary>
        /// <param name="startScan">
        /// The start scan.
        /// </param>
        /// <param name="endScan">
        /// The end scan.
        /// </param>
        /// <param name="startBin">
        /// The start bin.
        /// </param>
        /// <param name="endBin">
        /// The end bin.
        /// </param>
        /// <param name="xCompression">
        /// The x compression.
        /// </param>
        /// <param name="yCompression">
        /// The y compression.
        /// </param>
        /// <param name="fullScan">
        /// The full Scan.
        /// </param>
        /// <param name="exportFormat">
        /// The export Format.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool RunVoltageAccumulationWorkflow(int startScan, int endScan, int startBin, int endBin, double xCompression, double yCompression, bool fullScan, FileFormatEnum exportFormat)
        {
            CrossSectionSearchParameters defaultParams = new CrossSectionSearchParameters();
            VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(this.UimfReader, 100, defaultParams);
            IEnumerable<VoltageGroup> voltageGroups = accumulatedXiCs.Keys;
            bool success = true;
            foreach (var voltageGroup in voltageGroups)
            {
                // convert to MzML or UIMFs
                if (exportFormat == FileFormatEnum.UIMF)
                {
                    string outputPath = Path.Combine(this.OutputDir, this.datasetName + "_" + Math.Round(voltageGroup.MeanVoltageInVolts) + "V.uimf");
                    UimfExporter uimfExporter = new UimfExporter();
                    success = success && uimfExporter.ExportVoltageGroupAsSingleFrameUimf(outputPath, voltageGroup, this.UimfReader, this.averageNotSum, startScan, endScan, startBin, endBin, xCompression, yCompression, fullScan);
                    Console.WriteLine("Writing UIMF files to {0}", outputPath);
                }
                else if (exportFormat == FileFormatEnum.MzML)
                {
                    string outputPath = Path.Combine(this.OutputDir, this.datasetName + "_" + Math.Round(voltageGroup.MeanVoltageInVolts) + "V.mzML");
                    RetentionMobilitySwappedMzMLExporter mzMLExporter = new RetentionMobilitySwappedMzMLExporter();
                    success = success && mzMLExporter.ExportMzML(this.inputPath, outputPath, voltageGroup, this.UimfReader, this.averageNotSum);
                    Console.WriteLine("Writing MzML files to {0}", outputPath);
                }
            }

            return success;
        }

        public bool ExportToMzML(string MzMLPath)
        {
            XmlWriter writer = XmlWriter.Create(Console.Out);
            writer.WriteStartElement("Foo");
            writer.WriteAttributeString("Bar", "Some & value");
            writer.WriteElementString("Nested", "data");
            writer.WriteEndElement();
            return true;
        }
    }
}
