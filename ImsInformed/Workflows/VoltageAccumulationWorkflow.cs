// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VoltageAccumulationWorkflow.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the VoltageAccumulationWorkflow type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.IO;
    using ImsInformed.Parameters;
    using ImsInformed.Stats;

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

        private string datasetName;

        /// <summary>
        /// The UIMF reader.
        /// </summary>
        public readonly DataReader UimfReader;

        public VoltageAccumulationWorkflow(bool averageNotSum, string uimfLocation, string outputDirectory)
        {
            this.UimfReader = new DataReader(uimfLocation);
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

            this.OutputPath = outputDirectory;
            GlobalParams globalParam = this.UimfReader.GetGlobalParams();
            this.NumberOfFrames = globalParam.NumFrames;
            this.NumberOfBins = globalParam.GetValueInt32(GlobalParamKeyType.Bins);
        }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// The run voltage accumulation workflow.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool RunVoltageAccumulationWorkflow()
        {
            return this.RunVoltageAccumulationWorkflow(0, 0, true);
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
        public bool RunVoltageAccumulationWorkflow(int startScan, int endScan)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, false);
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
        public bool RunVoltageAccumulationWorkflow(int startScan, int endScan, int startBin, int endBin)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, startBin, endBin, false);
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
        public bool RunVoltageAccumulationWorkflow(int startScan, int endScan, int startBin, int endBin, double xCompression, double yCompression)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, startBin, endBin, xCompression, yCompression, false);
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
        private bool RunVoltageAccumulationWorkflow(int startScan, int endScan, bool fullScan)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, 1, this.NumberOfBins, fullScan);
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
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool RunVoltageAccumulationWorkflow(int startScan, int endScan, int startBin, int endBin, bool fullScan)
        {
            return this.RunVoltageAccumulationWorkflow(startScan, endScan, startBin, endBin, 1, 1, fullScan);
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
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool RunVoltageAccumulationWorkflow(int startScan, int endScan, int startBin, int endBin, double xCompression, double yCompression, bool fullScan)
        {
            CrossSectionSearchParameters defaultParams = new CrossSectionSearchParameters();
            VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(this.UimfReader, 100, defaultParams);
            IEnumerable<VoltageGroup> voltageGroups = accumulatedXiCs.Keys;
            bool success = true;
            foreach (var voltageGroup in voltageGroups)
            {
                int startingFrame = voltageGroup.FirstFrameNumber;
                int endingFrame = voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount - 1;

                FrameParams frameParam = this.UimfReader.GetFrameParams(startingFrame);
                if (fullScan)
                {
                    startScan = 1;
                    endScan = frameParam.Scans;
                }

                string fileName = this.datasetName + "_" + Math.Round(voltageGroup.MeanVoltageInVolts) + "V.uimf";

                double[,] summedIntensities = this.UimfReader.AccumulateFrameData(startingFrame, endingFrame, false, startScan, endScan, startBin, endBin, 1, 1);

                // convert to MzML or UIMFs
                UimfExporter exporter = new UimfExporter();
                success = success && exporter.ExportVoltageGroupAsSingleFrameUimf(Path.Combine(OutputPath, fileName), voltageGroup, this.UimfReader, summedIntensities, this.averageNotSum);
            }

            return true;
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
