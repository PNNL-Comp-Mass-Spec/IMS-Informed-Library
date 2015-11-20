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
namespace ImsInformed.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain;

    using MagnitudeConcavityPeakFinder;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    /// <summary>
    /// The peak finding.
    /// </summary>
    internal class PeakFinding
    {
        /// <summary>
        /// The find peak using MASIC.
        /// </summary>
        /// <param name="intensityPoints">
        /// The intensity points.
        /// </param>
        public static IList<clsPeak> FindPeakUsingMasic(List<IntensityPoint> intensityPoints, int totalScans)
        {
            PeakDetector.udtSICPeakFinderOptionsType option = TuneMasicOption();

            var paddedIntensityPoints = IMSUtil.PadZeroesToPointList(intensityPoints, totalScans);

            // Convert intensity points to key value pair
            List<KeyValuePair<int, double>> cartesianData = paddedIntensityPoints.Select(point => new KeyValuePair<int, double>(point.ScanIms, point.Intensity)).ToList();
            
            PeakDetector detector = new PeakDetector();
            List<double> smoothedYData;

            // Find the peaks
            List<clsPeak> detectedPeaks = detector.FindPeaks(option, cartesianData, 1, out smoothedYData);
            return detectedPeaks;
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
        /// <param name="featureFilterLevel">
        /// The feature Filter Level.
        /// </param>
        /// <returns>
        /// The <see cref="IList"/>.
        /// </returns>
        public static List<FeatureBlob> FindPeakUsingWatershed(List<IntensityPoint> intensityPoints, SavitzkyGolaySmoother smoother, double featureFilterLevel)
        {
            IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPoints);
            
            // Ignore the smoother for now as it distort peak apexs arbitrarily. 
            // smoother.Smooth(ref pointList);
            
            // Peak Find Chromatogram
            List<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList).ToList();
            
            // Preliminary filtering: reject small feature peaks.
            featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, featureFilterLevel).ToList();

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

            // Set the default ViperCompatibleMass Spectra noise threshold options
            peakFinderOptions.MassSpectraNoiseThresholdOptions = NoiseLevelAnalyzer.GetDefaultNoiseThresholdOptions();

            // Customize a few values
            peakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = NoiseLevelAnalyzer.eNoiseThresholdModes.TrimmedMedianByAbundance;
            peakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = 0.5f;
            peakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = 2;

            peakFinderOptions.SelectedIonMonitoringDataIsPresent = false;
            peakFinderOptions.ReturnClosestPeak = true;
            return peakFinderOptions;
        }

        /// <summary>
        /// Convert the MS feature from MASIC to standard feature from watershed.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <returns>
        /// The <see cref="StandardImsPeak"/>.
        /// </returns>
        private static StandardImsPeak NormalizeMASICFeature(clsPeak peak)
        {
            // Write it.
            // FeatureBlob watershedFeature = new FeatureBlob(0);
            // watershedFeature.PointList = new List<Point>();
            // 
            // Talor MASIC's sometimes asymetirc peak into watershed flavored peak.
            // 
            // watershedFeature.PointList blablabla
            throw new NotImplementedException();
        }
    }
}
