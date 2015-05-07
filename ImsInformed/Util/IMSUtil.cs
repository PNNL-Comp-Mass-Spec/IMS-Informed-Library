using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Util
{
    using ImsInformed.Domain.DirectInjection;

    using UIMFLibrary;

    public class IMSUtil
    {
        /// <summary>
        /// The pad zeroes to point list.
        /// </summary>
        /// <param name="sparsePointList">
        /// The sparse point list. Note that this method is written for sorrted 1D data where LcScan is invariant.
        /// </param>
        /// <param name="destinationSize">
        /// The destination size.
        /// </param>
        /// <returns>
        /// The <see cref="IList{T}"/>.
        /// </returns>
        public static IList<IntensityPoint> PadZeroesToPointList(IList<IntensityPoint> sparsePointList, int destinationSize)
        {
            if (destinationSize < sparsePointList.Last().ScanIms)
            {
                throw new InvalidOperationException("destinationSize has to be greater then original size");
            }

            IList<IntensityPoint> newList = new List<IntensityPoint>();
            int lc = sparsePointList.First().ScanLc;

            int oldListIndex = 0;
            for (int scanIms = 1; scanIms <= destinationSize; scanIms++)
            {
                if (oldListIndex >= sparsePointList.Count || scanIms < sparsePointList[oldListIndex].ScanIms)
                {
                    newList.Add(new IntensityPoint(lc, scanIms, 0));
                } 
                else if (scanIms == sparsePointList[oldListIndex].ScanIms)
                {
                    newList.Add(sparsePointList[oldListIndex]);
                    oldListIndex++;
                }
                else
                {
                    throw new InvalidOperationException("Logical error");
                }
            }

            return newList;
        }

        /// <summary>
        /// The is last voltage group.
        /// </summary>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <param name="totalFrames">
        /// The total frames.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsLastVoltageGroup(VoltageGroup group, int totalFrames)
        {
            return @group.LastFrameNumber == totalFrames;
        }

        /// <summary>
        /// The de normalize drift time.
        /// </summary>
        /// <param name="driftTime">
        /// The drift time.
        /// </param>
        /// <param name="pressureInTorr">
        /// The pressure in torr.
        /// </param>
        /// <param name="TemperatureInKelvin">
        /// The temperature in kelvin.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double DeNormalizeDriftTime(double driftTime, double pressureInTorr, double TemperatureInKelvin)
        {
            double normalizedPressure = pressureInTorr / UnitConversion.StandardImsPressureInTorr;
            double normalizedTemperature = TemperatureInKelvin / UnitConversion.RoomTemperatureInKelvin;
            return driftTime * normalizedPressure;
        }

        public static double DeNormalizeDriftTime(double driftTime, VoltageGroup group)
        {
            double normalizedPressure = UnitConversion.Nondimensionalized2Torr(group.MeanPressureNondimensionalized) / UnitConversion.StandardImsPressureInTorr;
            double normalizedTemperature = UnitConversion.Nondimensionalized2Kelvin(group.MeanTemperatureNondimensionalized) / UnitConversion.RoomTemperatureInKelvin;
            return driftTime * normalizedPressure;
        }

        /// <summary>
        /// The normalize drift time.
        /// </summary>
        /// <param name="driftTime">
        /// The drift time.
        /// </param>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double NormalizeDriftTime(double driftTime, VoltageGroup group)
        {
            double normalizedPressure = UnitConversion.Nondimensionalized2Torr(group.MeanPressureNondimensionalized) / UnitConversion.StandardImsPressureInTorr;
            double normalizedTemperature = UnitConversion.Nondimensionalized2Kelvin(group.MeanTemperatureNondimensionalized) / UnitConversion.RoomTemperatureInKelvin;
            return driftTime / normalizedPressure;
        }

        /// <summary>
        /// return the maximum intensity value possible for a given voltage group
        /// Note currently supports 8-bit digitizers, proceeds with caution when
        /// dealing with 12-bit digitizers
        /// </summary>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double MaxIntensityAfterFrameAccumulation(VoltageGroup group, DataReader reader)
        {
            return 255 * group.FrameAccumulationCount * reader.GetFrameParams(group.FirstFrameNumber).GetValueInt32(FrameParamKeyType.Accumulations);
        }

        /// <summary>
        /// Since loading all the data onto the screen can overload the computer
        /// get max global intensities locally then compare.
        /// </summary>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        [Obsolete("This method is really slow. Use MaxIntensityAfterFrameAccumulation instead")]
        public static double MaxGlobalIntensities(VoltageGroup group, DataReader reader)
        {
            GlobalParams global = reader.GetGlobalParams();
            FrameParams param = reader.GetFrameParams(@group.FirstFrameNumber);
            int firstFrame = group.FirstFrameNumber;
            int lastFrame = group.LastFrameNumber;
            int firstScan = 1;
            int lastScan = param.Scans;
            int firstBin = 0;
            int lastBin = global.Bins;
            int windowOfBins = 1000;
            int windowOfScans = 20;
            double maxIntensities = 0;
            for (int scan = firstScan; scan <= lastScan; scan += windowOfScans)
            {
                int endScan = (scan + windowOfScans > lastScan) ? lastScan : scan + windowOfScans;
                for (int bin = firstBin; bin <= lastBin; bin += windowOfBins)
                {
                    int endBin = (bin + windowOfBins > lastBin) ? lastBin : bin + windowOfBins;
    
                    double localMaxIntensities = MaxGlobalIntensities(reader, scan, endScan, bin, endBin, firstFrame, lastFrame);
                    maxIntensities = (localMaxIntensities > maxIntensities) ? localMaxIntensities : maxIntensities;
                }
            }
            return maxIntensities;
        }

        /// <summary>
        /// Calculate the max global intensities for the given voltage group.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <param name="firstScan">
        /// The first scan.
        /// </param>
        /// <param name="lastScan">
        /// The last scan.
        /// </param>
        /// <param name="firstBin">
        /// The first bin.
        /// </param>
        /// <param name="lastBin">
        /// The last bin.
        /// </param>
        /// <param name="firstFrame">
        /// The first frame.
        /// </param>
        /// <param name="lastFrame">
        /// The last frame.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private static double MaxGlobalIntensities(DataReader reader, int firstScan, int lastScan, int firstBin, int lastBin, int firstFrame, int lastFrame)
        {
            int[][][] intensities = reader.GetIntensityBlock(firstFrame, lastFrame, DataReader.FrameType.MS1, firstScan, lastScan, firstBin, lastBin);
            long maxIntensities = 0;
            for (int scan = firstScan; scan <= lastScan; scan++)
            {
                for (int bin = firstBin; bin < lastBin; bin++)
                {
                    long sumIntensity = 0;
                    for (int frame = firstFrame; frame <= lastFrame; frame++)
                    {
                        sumIntensity += intensities[frame - 1][scan - firstScan][bin - firstBin];
                    }
                    maxIntensities = (sumIntensity > maxIntensities) ? sumIntensity : maxIntensities;
                }
            }

            return maxIntensities;
        }
    }
}
