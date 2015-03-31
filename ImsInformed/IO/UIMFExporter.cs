// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UIMFExporter.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The uimf exporter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.IO
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using DeconTools.Backend.Utilities;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Stats;

    using MathNet.Numerics.Integration.Algorithms;

    using UIMFLibrary;

    /// <summary>
    /// The uimf exporter.
    /// </summary>
    public class UimfExporter
    {
        /// <summary>
        /// The export voltage group as single frame UIMF.
        /// </summary>
        /// <param name="outputPath">
        /// The output path.
        /// </param>
        /// <param name="voltageGroup">
        /// The voltage group.
        /// </param>
        /// <param name="originalUIMF">
        /// The original uimf.
        /// </param>
        /// <param name="averageNotSum">
        /// The average not sum.
        /// </param>
        /// <param name="startScan">
        /// The start Scan.
        /// </param>
        /// <param name="endScan">
        /// The end Scan.
        /// </param>
        /// <param name="startBin">
        /// The start Bin.
        /// </param>
        /// <param name="endBin">
        /// The end Bin.
        /// </param>
        /// <param name="xCompression">
        /// The x Compression.
        /// </param>
        /// <param name="yCompression">
        /// The y Compression.
        /// </param>
        /// <param name="fullScan">
        /// The full Scan.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool ExportVoltageGroupAsSingleFrameUimf(string outputPath, VoltageGroup voltageGroup, DataReader originalUIMF, bool averageNotSum, int startScan, int endScan, int startBin, int endBin, double xCompression, double yCompression, bool fullScan)
        {
            int startingFrame = voltageGroup.FirstFrameNumber;
            int endingFrame = voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount - 1;

                FrameParams frameParam = originalUIMF.GetFrameParams(startingFrame);
                if (fullScan)
                {
                    startScan = 1;
                    endScan = frameParam.Scans;
                }

                Console.Write("Summing frame[{0} - {1}]...    ", startingFrame, endingFrame);
                double[,] summedIntensities = originalUIMF.AccumulateFrameData(startingFrame, endingFrame, false, startScan, endScan, startBin, endBin, xCompression, yCompression);

            int scans = summedIntensities.GetLength(0);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            using (var uimfWriter = new DataWriter(outputPath))
            {
                uimfWriter.CreateTables(null);
                GlobalParams newGlobalParams = originalUIMF.GetGlobalParams();

                // Update some fields in the new UIMFs
                newGlobalParams.AddUpdateValue(GlobalParamKeyType.DateStarted, DateTime.Now);
                newGlobalParams.AddUpdateValue(GlobalParamKeyType.NumFrames, 1);
                if (averageNotSum)
                {
                    int postProcessingAccumulation = averageNotSum ? 1 : voltageGroup.AccumulationCount;
                    int totalAccumulation = newGlobalParams.GetValueInt32(GlobalParamKeyType.PrescanAccumulations) * postProcessingAccumulation;
                    newGlobalParams.AddUpdateValue(GlobalParamKeyType.PrescanAccumulations, totalAccumulation);
                }
                
                uimfWriter.InsertGlobal(newGlobalParams);

                // Insert the single frame.
                FrameParams newFrameParams = originalUIMF.GetFrameParams(voltageGroup.FirstFrameNumber);
                if (averageNotSum)
                {
                    int postProcessingAccumulation = averageNotSum ? 1 : voltageGroup.AccumulationCount;
                    int totalAccumulation = newGlobalParams.GetValueInt32(GlobalParamKeyType.PrescanAccumulations) * postProcessingAccumulation;
                    newFrameParams.AddUpdateValue(FrameParamKeyType.Accumulations, totalAccumulation);
                }

                newFrameParams.AddUpdateValue(FrameParamKeyType.AmbientTemperature, UnitConversion.Nondimensionalized2Kelvin(voltageGroup.MeanTemperatureNondimensionalized)); 
                newFrameParams.AddUpdateValue(FrameParamKeyType.FloatVoltage, voltageGroup.MeanVoltageInVolts); 
                newFrameParams.AddUpdateValue(FrameParamKeyType.PressureBack, UnitConversion.Nondimensionalized2Torr(voltageGroup.MeanPressureNondimensionalized)); 

                uimfWriter.InsertFrame(1, newFrameParams);
                
                // Insert the scans
                for (int scan = 1; scan <= scans; scan++)
                {
                    var scanData = this.GetScan(summedIntensities, scan);
                
                    uimfWriter.InsertScan(
                        1,
                        newFrameParams,
                        scan,
                        scanData,
                        newGlobalParams.BinWidth);
                }

                // Add legacy tables
                uimfWriter.AddLegacyParameterTablesUsingExistingParamTables();

                return true;
            }
        }

        private IEnumerable<int> GetScan(double[,] intensities, int scanNumber)
        {
            int scans = intensities.GetLength(0);
            int bins = intensities.GetLength(1);
            if (scanNumber > scans || scanNumber < 1)
            {
                throw new IndexOutOfRangeException();
            }

            List<int> scanData = new List<int>();
            for (int bin = 0; bin < bins; bin++)
            {
                scanData.Add((int)Math.Round(intensities[scanNumber - 1, bin]));
            }

            return scanData;
        }
    }
}
