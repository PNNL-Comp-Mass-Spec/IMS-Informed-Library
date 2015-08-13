// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrossSectionSearchParameters.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The molecule workflow parameters.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using ImsInformed.Util;

    /// <summary>
    /// The molecule workflow parameters.
    /// </summary>
    public class CrossSectionSearchParameters
    {
        public const double DefaultDriftTimeToleranceInMs = 0.5;
        public const double DefaultMzWindowHalfWidthInPpm = 250;
        public const int DefaultNumPointForSmoothing = 9;
        public const double DefaultFeatureFilterLevel = 0.25;
        public const double DefaultAbsoluteIntensityThreshold = 0.00; 
        public const double DefaultRelativeIntensityPercentageThreshold = 4; 
        public const double DefaultPeakShapeThreshold = 0.4;
        public const double DefaultIsotopicThreshold = 0.4;
        public const int DefaultMinFitPoints = 4;
        public const PeakDetectorEnum DefaultPeakDetectorSelection =  PeakDetectorEnum.WaterShed;
        public const double DefaultMinR2 = 0.98;
        public const double DefaultConformerMzTolerance = 20;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        public CrossSectionSearchParameters() : this(DefaultDriftTimeToleranceInMs, 
            DefaultMzWindowHalfWidthInPpm, DefaultNumPointForSmoothing, DefaultFeatureFilterLevel, 
            DefaultAbsoluteIntensityThreshold, DefaultPeakShapeThreshold, DefaultIsotopicThreshold, 
            DefaultMinFitPoints, DefaultPeakDetectorSelection, 
            DefaultMinR2, DefaultConformerMzTolerance, DefaultRelativeIntensityPercentageThreshold)
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        /// <param name="driftTimeToleranceInMs">
        /// The drift Time Tolerance In Ms.
        /// </param>
        /// <param name="mzWindowHalfWidthInPpm">
        /// The mass tolerance in ppm.
        /// </param>
        /// <param name="numPointForSmoothing">
        /// The num point for smoothing.
        /// </param>
        /// <param name="featureFilterLevel">
        /// The feature filter level.
        /// </param>
        /// <param name="absoluteIntensityThreshold">
        /// The intensity threshold.
        /// </param>
        /// <param name="peakShapeThreshold">
        /// The peak shape threshold.
        /// </param>
        /// <param name="isotopicThreshold">
        /// The isotopic threshold.
        /// </param>
        /// <param name="minFitPoints">
        /// The min fit points.
        /// </param>
        /// <param name="expectIsomer">
        /// The expect Isomer.
        /// </param>
        /// <param name="peakDetectorSelection">
        /// The peak Detector Selection.
        /// </param>
        /// <param name="minR2">
        /// The min R 2.
        /// </param>
        /// <param name="conformerMzTolerance"></param>
        /// <param name="relativeIntensityPercentageThreshold"></param>
        public CrossSectionSearchParameters(double driftTimeToleranceInMs, double mzWindowHalfWidthInPpm, int numPointForSmoothing, double featureFilterLevel, double absoluteIntensityThreshold, double peakShapeThreshold, double isotopicThreshold, int minFitPoints, PeakDetectorEnum peakDetectorSelection, double minR2, double conformerMzTolerance, double relativeIntensityPercentageThreshold)
        {
            this.ConformerMzTolerance = conformerMzTolerance;
            this.DriftTimeToleranceInMs = driftTimeToleranceInMs;
            this.NumPointForSmoothing = numPointForSmoothing;
            this.MzWindowHalfWidthInPpm = mzWindowHalfWidthInPpm;
            this.FeatureFilterLevel = featureFilterLevel;
            this.AbsoluteIntensityThreshold = absoluteIntensityThreshold;
            this.PeakShapeThreshold = peakShapeThreshold;
            this.IsotopicThreshold = isotopicThreshold;
            this.MinFitPoints = minFitPoints;
            this.PeakDetectorSelection = peakDetectorSelection;
            this.MinR2 = minR2;
            this.RelativeIntensityPercentageThreshold = relativeIntensityPercentageThreshold;
        }

        /// <summary>
        /// Gets or sets the mass tolerance in ppm.
        /// </summary>
        public double MzWindowHalfWidthInPpm { get; private set; }

        /// <summary>
        /// Gets or sets the feature filter level.
        /// </summary>
        public double FeatureFilterLevel { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double AbsoluteIntensityThreshold { get; private set; }

         /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double RelativeIntensityPercentageThreshold { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double ConformerMzTolerance { get; private set; }
        
        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double PeakShapeThreshold { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double IsotopicThreshold { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public int MinFitPoints { get; private set; }

        /// <summary>
        /// Gets or sets the number point for smoothing.
        /// </summary>
        public int NumPointForSmoothing { get; private set; }

        /// <summary>
        /// Gets or sets the scan window width.
        /// </summary>
        public double DriftTimeToleranceInMs { get; private set; }

        /// <summary>
        /// Gets the peak detector selection.
        /// </summary>
        public PeakDetectorEnum PeakDetectorSelection { get; private set; }

        /// <summary>
        /// The min r 2.
        /// </summary>
        public double MinR2 { get; private set; }
    }
}
