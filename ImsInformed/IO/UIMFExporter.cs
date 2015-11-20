// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
namespace ImsInformed.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Util;

    using UIMFLibrary;

    /// <summary>
    /// The uimf exporter.
    /// </summary>
    internal class UimfExporter
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
            int endingFrame = voltageGroup.LastFrameNumber;

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
                    int postProcessingAccumulation = averageNotSum ? 1 : voltageGroup.FrameAccumulationCount;
                    int totalAccumulation = newGlobalParams.GetValueInt32(GlobalParamKeyType.PrescanAccumulations) * postProcessingAccumulation;
                    newGlobalParams.AddUpdateValue(GlobalParamKeyType.PrescanAccumulations, totalAccumulation);
                }
                
                uimfWriter.InsertGlobal(newGlobalParams);

                // Insert the single frame.
                FrameParams newFrameParams = originalUIMF.GetFrameParams(voltageGroup.FirstFrameNumber);
                if (averageNotSum)
                {
                    int postProcessingAccumulation = averageNotSum ? 1 : voltageGroup.FrameAccumulationCount;
                    int totalAccumulation = newGlobalParams.GetValueInt32(GlobalParamKeyType.PrescanAccumulations) * postProcessingAccumulation;
                    newFrameParams.AddUpdateValue(FrameParamKeyType.Accumulations, totalAccumulation);
                }

                newFrameParams.AddUpdateValue(FrameParamKeyType.AmbientTemperature, Metrics.Nondimensionalized2Kelvin(voltageGroup.MeanTemperatureNondimensionalized)); 
                newFrameParams.AddUpdateValue(FrameParamKeyType.FloatVoltage, voltageGroup.MeanVoltageInVolts); 
                newFrameParams.AddUpdateValue(FrameParamKeyType.PressureBack, Metrics.Nondimensionalized2Torr(voltageGroup.MeanPressureNondimensionalized)); 

                uimfWriter.InsertFrame(1, newFrameParams);
                
                // Insert the scans
                for (int scan = 1; scan <= scans; scan++)
                {
                    var scanData = this.GetScan(summedIntensities, scan);
                
                    uimfWriter.InsertScan(
                        1,
                        newFrameParams,
                        scan,
                        scanData.ToList(),
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
