// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrossSectionSearchParameters.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The molecule workflow parameters.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Parameters
{
    /// <summary>
    /// The molecule workflow parameters.
    /// </summary>
    public class CrossSectionSearchParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        public CrossSectionSearchParameters()
        {
            
            this.MassToleranceInPpm = 10;
            this.NumPointForSmoothing = 9;
            this.FeatureFilterLevel = 0.25;
            this.IntensityThreshold = 0.00;
            this.PeakShapeThreshold = 0.4;
            this.IsotopicThreshold = 0.4;
            this.MinFitPoints = 3;
            this.ScanWindowWidth = 4;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        /// <param name="scanWindowWidth">
        /// The scan window width.
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
        public CrossSectionSearchParameters(int scanWindowWidth, double massToleranceInPpm, int numPointForSmoothing, double featureFilterLevel, double intensityThreshold, double peakShapeThreshold, double isotopicThreshold, int minFitPoints)
        {
            this.ScanWindowWidth = scanWindowWidth;
            this.NumPointForSmoothing = numPointForSmoothing;
            this.MassToleranceInPpm = massToleranceInPpm;
            this.FeatureFilterLevel = featureFilterLevel;
            this.IntensityThreshold = intensityThreshold;
            this.PeakShapeThreshold = peakShapeThreshold;
            this.IsotopicThreshold = isotopicThreshold;
            this.MinFitPoints = minFitPoints;
        }

        /// <summary>
        /// Gets or sets the mass tolerance in ppm.
        /// </summary>
        public double MassToleranceInPpm { get; private set; }

        /// <summary>
        /// Gets or sets the feature filter level.
        /// </summary>
        public double FeatureFilterLevel { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double IntensityThreshold { get; private set; }

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
        public int ScanWindowWidth { get; private set; }
    }
}
