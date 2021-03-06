﻿// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
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
namespace ImsInformed.Domain.DirectInjection
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Controls;

    using ImsInformed.Util;

    using UIMFLibrary;

    /// <summary>
    /// This is the XIC summed over the centerMz but not drift time axis.
    /// </summary>
    internal class ExtractedIonChromatogram 
    {
        public ExtractedIonChromatogram()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractedIonChromatogram"/> class. 
        /// Get the extracted ion chromatogram from the whole drift time scan range.
        /// </summary>
        /// <param name="uimfReader">
        /// The UIMF reader.
        /// </param>
        /// <param name="frameNumber">
        /// The frame number.
        /// </param>
        /// <param name="centerMz">
        /// The MZ.
        /// </param>
        /// <param name="massToleranceInPpm">
        /// The mass Tolerance In Ppm.
        /// </param>
        public ExtractedIonChromatogram(DataReader uimfReader, int frameNumber, double centerMz, double massToleranceInPpm)
        {
            FrameParams param = uimfReader.GetFrameParams(frameNumber);

            this.IntensityPoints = uimfReader.GetXic(
                    centerMz,
                    massToleranceInPpm,
                    frameNumber,
                    frameNumber,
                    1,
                    param.Scans,
                    DataReader.FrameType.MS1,
                    DataReader.ToleranceType.PPM);

            this.CenterMz = centerMz;
            this.MassToleranceInPpm = massToleranceInPpm;
            this.NumberOfMobilityScans = param.Scans;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractedIonChromatogram"/> class.
        /// Get the extracted ion chromatogram from the a particular drift time scan range.
        /// </summary>
        /// <param name="uimfReader">
        /// The uimf reader.
        /// </param>
        /// <param name="frameNumber">
        /// The frame number.
        /// </param>
        /// <param name="centerMz">
        /// The center mz.
        /// </param>
        /// <param name="massToleranceInPpm">
        /// The mass tolerance in ppm.
        /// </param>
        /// <param name="centerDriftTimeInMs">
        /// The center drift time.
        /// </param>
        /// <param name="driftTimeErrorInMs">
        /// The drift time error in ms.
        /// </param>
        public ExtractedIonChromatogram(DataReader uimfReader, int frameNumber, double centerMz, double massToleranceInPpm, double centerDriftTimeInMs, double driftTimeErrorInMs)
        {
            FrameParams param = uimfReader.GetFrameParams(frameNumber);

            double driftTimeMin = centerDriftTimeInMs - driftTimeErrorInMs;

            double driftTimeMax = centerDriftTimeInMs + driftTimeErrorInMs;

            double scanWidthInSeconds = param.GetValueDouble(FrameParamKeyType.AverageTOFLength) / 1000000000;

            int scanNumberMin = Metrics.DriftTimeInMsToNearestImsScanNumber(driftTimeMin, scanWidthInSeconds, param.Scans);
            int scanNumberMax = Metrics.DriftTimeInMsToNearestImsScanNumber(driftTimeMax, scanWidthInSeconds, param.Scans);

            this.IntensityPoints = uimfReader.GetXic(
                    centerMz,
                    massToleranceInPpm,
                    frameNumber,
                    frameNumber,
                    scanNumberMin,
                    scanNumberMax,
                    DataReader.FrameType.MS1,
                    DataReader.ToleranceType.PPM);

            this.CenterMz = centerMz;
            this.MassToleranceInPpm = massToleranceInPpm;
            this.NumberOfMobilityScans = param.Scans;
        }

        public static ExtractedIonChromatogram AverageXICFromFrames(DataReader uimfReader, int frameNumberFrom, int frameNumberTo, double centerMz, double massToleranceInPpm, double centerDriftTimeInMs, double driftTimeErrorInMs)
        {
            ExtractedIonChromatogram result = new ExtractedIonChromatogram();
            for (     int i = frameNumberFrom; i <= frameNumberTo; i++)
            {
                var xic = new ExtractedIonChromatogram(uimfReader, i, centerMz, massToleranceInPpm, centerDriftTimeInMs, driftTimeErrorInMs);
                if (i == frameNumberFrom)
                {
                    result = xic;
                }
                else
                {
                    result += xic;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractedIonChromatogram"/> class. 
        /// XIC is a list of intensity points sorted by Mobility Scan number from low to high.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        private ExtractedIonChromatogram(ExtractedIonChromatogram a, ExtractedIonChromatogram b)
        {
            if (a.NumberOfMobilityScans != b.NumberOfMobilityScans || !a.CenterMz.Equals(b.CenterMz))
            {
                throw new InvalidOperationException("Cannot sum XICs with different number of mobilities or different MZ.");
            }

            this.CenterMz = a.CenterMz;
            this.NumberOfMobilityScans = b.NumberOfMobilityScans;
            this.IntensityPoints = addSortedIntensityPointList(a.IntensityPoints, b.IntensityPoints, this.NumberOfMobilityScans);
        }

        /// <summary>
        /// Gets the MZ.
        /// </summary>
        public double CenterMz {get; private set; }

        /// <summary>
        /// Gets the mass tolerance in ppm.
        /// </summary>
        public double MassToleranceInPpm { get; private set; }

        /// <summary>
        /// Gets the intensity points.
        /// </summary>
        public List<IntensityPoint> IntensityPoints { get; private set; }

        /// <summary>
        /// Gets the number of mobility scans.
        /// </summary>
        public int NumberOfMobilityScans { get; private set; }

        /// <summary>
        /// The +.
        /// </summary>
        /// <param name="A">
        /// The a.
        /// </param>
        /// <param name="B">
        /// The b.
        /// </param>
        /// <returns>
        /// </returns>
        public static ExtractedIonChromatogram operator +(ExtractedIonChromatogram A, ExtractedIonChromatogram B)
        {
            ExtractedIonChromatogram result = new ExtractedIonChromatogram(A, B);
            return result;
        }

        /// <summary>
        /// Add sorted intensity point list. Classical algorithm for merging 2 sorted list.
        /// </summary>
        /// <param name="A">
        /// The a.
        /// </param>
        /// <param name="B">
        /// The b.
        /// </param>
        /// <param name="numberOfMobilityScans">
        /// The number of mobility scans.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        public static List<IntensityPoint> addSortedIntensityPointList (List<IntensityPoint> A, List<IntensityPoint> B, int numberOfMobilityScans)
        {
            List<IntensityPoint> result = new List<IntensityPoint>();
            int AIndex = 0;
            int BIndex = 0;
            while (AIndex < A.Count || BIndex < B.Count) 
            {
                if (BIndex >= B.Count && AIndex < A.Count)
                {
                    result.Add(new IntensityPoint(0, A[AIndex].ScanIms, A[AIndex].Intensity));
                    AIndex++;
                }
                else if (BIndex < B.Count && AIndex >= A.Count)
                {
                    result.Add(new IntensityPoint(0, B[BIndex].ScanIms, B[BIndex].Intensity)); 
                    BIndex++;
                }
                else if (A[AIndex].ScanIms < B[BIndex].ScanIms)
                {
                    result.Add(new IntensityPoint(0, A[AIndex].ScanIms, A[AIndex].Intensity));
                    AIndex++;
                } 
                else if (A[AIndex].ScanIms > B[BIndex].ScanIms)
                {
                    result.Add(new IntensityPoint(0, B[BIndex].ScanIms, B[BIndex].Intensity)); 
                    BIndex++;
                }
                else if (A[AIndex].ScanIms == B[BIndex].ScanIms)
                {
                    result.Add(new IntensityPoint(0, A[AIndex].ScanIms, A[AIndex].Intensity + B[BIndex].Intensity));
                    AIndex++;
                    BIndex++;
                }
            }

            return result;
        }

        /// <summary>
        /// The verify frame number.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="frameNumber">
        /// The frame number.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public static void VerifyFrameNumber(IntensityPoint point, int frameNumber)
        {
            if (point.ScanLc != frameNumber)
            {
                    throw new InvalidOperationException("Inconsistent FrameNumber for IntensityPointLists.");
            }
        }
    }
}
