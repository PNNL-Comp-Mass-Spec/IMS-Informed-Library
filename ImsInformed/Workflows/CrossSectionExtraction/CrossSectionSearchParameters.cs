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
        public const double DefaultMassToleranceInPpm = 250;
        public const int DefaultNumPointForSmoothing = 9;
        public const double DefaultFeatureFilterLevel = 0.25;
        public const double DefaultIntensityThreshold = 0.00; 
        public const double DefaultPeakShapeThreshold = 0.4;
        public const double DefaultIsotopicThreshold = 0.4;
        public const int DefaultMinFitPoints = 3;
        public const PeakDetectorEnum DefaultPeakDetectorSelection =  PeakDetectorEnum.WaterShed;
        public const double DefaultMinR2 = 0.98;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        public CrossSectionSearchParameters() : this(DefaultDriftTimeToleranceInMs, 
            DefaultMassToleranceInPpm, DefaultNumPointForSmoothing, DefaultFeatureFilterLevel, 
            DefaultIntensityThreshold, DefaultPeakShapeThreshold, DefaultIsotopicThreshold, 
            DefaultMinFitPoints, DefaultPeakDetectorSelection, 
            DefaultMinR2)
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        /// <param name="driftTimeToleranceInMs">
        /// The drift Time Tolerance In Ms.
        /// </param>
        /// <param name="massToleranceInPpm">
        /// The mass tolerance in ppm.
        /// </param>
        /// <param name="numPointForSmoothing">
        /// The num point for smoothing.
        /// </param>
        /// <param name="featureFilterLevel">
        /// The feature filter level.
        /// </param>
        /// <param name="intensityThreshold">
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
        public CrossSectionSearchParameters(double driftTimeToleranceInMs, double massToleranceInPpm, int numPointForSmoothing, double featureFilterLevel, double intensityThreshold, double peakShapeThreshold, double isotopicThreshold, int minFitPoints, PeakDetectorEnum peakDetectorSelection, double minR2)
        {
            this.DriftTimeToleranceInMs = driftTimeToleranceInMs;
            this.NumPointForSmoothing = numPointForSmoothing;
            this.MassToleranceInPpm = massToleranceInPpm;
            this.FeatureFilterLevel = featureFilterLevel;
            this.IntensityThreshold = intensityThreshold;
            this.PeakShapeThreshold = peakShapeThreshold;
            this.IsotopicThreshold = isotopicThreshold;
            this.MinFitPoints = minFitPoints;
            this.PeakDetectorSelection = peakDetectorSelection;
            this.MinR2 = minR2;
        }

        /// <summary>
        /// Gets or sets the mass tolerance in ppm.
        /// </summary>
        public double MassToleranceInPpm { get; set; }

        /// <summary>
        /// Gets or sets the feature filter level.
        /// </summary>
        public double FeatureFilterLevel { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double IntensityThreshold { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double PeakShapeThreshold { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double IsotopicThreshold { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public int MinFitPoints { get; set; }

        /// <summary>
        /// Gets or sets the number point for smoothing.
        /// </summary>
        public int NumPointForSmoothing { get; set; }

        /// <summary>
        /// Gets or sets the scan window width.
        /// </summary>
        public double DriftTimeToleranceInMs { get; set; }

        /// <summary>
        /// Gets the peak detector selection.
        /// </summary>
        public PeakDetectorEnum PeakDetectorSelection { get; set; }

        /// <summary>
        /// The min r 2.
        /// </summary>
        public double MinR2 { get; set; }
    }
}
