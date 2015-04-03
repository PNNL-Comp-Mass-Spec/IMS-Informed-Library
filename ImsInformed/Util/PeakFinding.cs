// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PeakFinding.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the PeakFinding type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Workflows.CrossSectionExtraction;

    using log4net.Util;

    using MagnitudeConcavityPeakFinder;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    /// <summary>
    /// The peak finding.
    /// </summary>
    public class PeakFinding
    {
        /// <summary>
        /// The find peak using MASIC.
        /// </summary>
        /// <param name="intensityPoints">
        /// The intensity points.
        /// </param>
        public static void FindPeakUsingMasic(List<IntensityPoint> intensityPoints, int totalScans)
        {
            PeakDetector.udtSICPeakFinderOptionsType option = TuneMasicOption();

            var paddedIntensityPoints = IMSUtil.PadZeroesToPointList(intensityPoints, totalScans);

            // Convert intensity points to key value pair
            List<KeyValuePair<int, double>> cartesianData = paddedIntensityPoints.Select(point => new KeyValuePair<int, double>(point.ScanIms, point.Intensity)).ToList();
            
            PeakDetector detector = new PeakDetector();
            List<double> smoothedYData;

            // Find the peaks
            List<clsPeak> detectedPeaks = detector.FindPeaks(option, cartesianData, 1, out smoothedYData);

            IList<FeatureBlob> features = new List<FeatureBlob>();

            // Display the peaks
            int peakNumber = 0;
            foreach (var peak in detectedPeaks)
            {
                FeatureBlob feature = new FeatureBlob(peakNumber);

                peakNumber++;
                Console.WriteLine("Peak " + peakNumber);
                Console.WriteLine("  Location =  " + peak.LocationIndex);
                Console.WriteLine("  Width =     " + peak.PeakWidth + " points");
                Console.WriteLine("  Area =      " + peak.Area.ToString("0.00"));
                Console.WriteLine("  LeftEdge =  " + peak.LeftEdge);
                Console.WriteLine("  RightEdge = " + peak.RightEdge);
                features.Add(feature);
            }

            // Convert the peakList to feature blob list
            
        }

        /// <summary>
        /// The find peak using watershed.
        /// </summary>
        /// <param name="intensityPoints">
        /// The intensity Points.
        /// </param>
        /// <param name="smoother">
        /// The smoother.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <returns>
        /// The <see cref="IList"/>.
        /// </returns>
        public static List<FeatureBlob> FindPeakUsingWatershed(List<IntensityPoint> intensityPoints, SavitzkyGolaySmoother smoother, CrossSectionSearchParameters parameters)
        {
            // Smooth Chromatogram
            IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPoints);
            smoother.Smooth(ref pointList);
            
            // Peak Find Chromatogram
            List<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList).ToList();
            
            // Preliminary filtering: reject small feature peaks.
            featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, parameters.FeatureFilterLevel).ToList();

            return featureBlobs;
        }

        private static PeakDetector.udtSICPeakFinderOptionsType TuneMasicOption()
        {
            PeakDetector.udtSICPeakFinderOptionsType peakFinderOptions = new PeakDetector.udtSICPeakFinderOptionsType
            {
                IntensityThresholdFractionMax = 0.01f,  // 1% of the peak maximum
                IntensityThresholdAbsoluteMinimum = 0,
                SICBaselineNoiseOptions = NoiseLevelAnalyzer.GetDefaultNoiseThresholdOptions()
            };

            // Customize a few values
            peakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = NoiseLevelAnalyzer.eNoiseThresholdModes.TrimmedMedianByAbundance;

            peakFinderOptions.MaxDistanceScansNoOverlap = 0;

            peakFinderOptions.MaxAllowedUpwardSpikeFractionMax = 0.2f;  // 20%

            peakFinderOptions.InitialPeakWidthScansScaler = 1;
            peakFinderOptions.InitialPeakWidthScansMaximum = 30;

            peakFinderOptions.FindPeaksOnSmoothedData = true;
            peakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = true;

            // If this is true, will ignore UseSavitzkyGolaySmooth
            peakFinderOptions.UseButterworthSmooth = true;

            peakFinderOptions.ButterworthSamplingFrequency = 0.25f;
            peakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData = true;

            peakFinderOptions.UseSavitzkyGolaySmooth = false;

            // Moving average filter if 0, Savitzky Golay filter if 2, 4, 6, etc.
            peakFinderOptions.SavitzkyGolayFilterOrder = 2;

            // Set the default Mass Spectra noise threshold options
            peakFinderOptions.MassSpectraNoiseThresholdOptions = NoiseLevelAnalyzer.GetDefaultNoiseThresholdOptions();

            // Customize a few values
            peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = NoiseLevelAnalyzer.eNoiseThresholdModes.TrimmedMedianByAbundance;
            peakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = 0.5f;
            peakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = 2;

            peakFinderOptions.SelectedIonMonitoringDataIsPresent = false;
            peakFinderOptions.ReturnClosestPeak = true;
            return peakFinderOptions;
        }
    }
}
