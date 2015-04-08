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
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSearchParameters"/> class.
        /// </summary>
        public CrossSectionSearchParameters() : this(0.5, 10, 9, 0.25, 0.00, 0.4, 0.4, 3, false, PeakDetectorEnum.WaterShed)
        { 
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
        public CrossSectionSearchParameters(double driftTimeToleranceInMs, double massToleranceInPpm, int numPointForSmoothing, double featureFilterLevel, double intensityThreshold, double peakShapeThreshold, double isotopicThreshold, int minFitPoints, bool expectIsomer, PeakDetectorEnum peakDetectorSelection)
        {
            this.DriftTimeToleranceInMs = driftTimeToleranceInMs;
            this.NumPointForSmoothing = numPointForSmoothing;
            this.MassToleranceInPpm = massToleranceInPpm;
            this.FeatureFilterLevel = featureFilterLevel;
            this.IntensityThreshold = intensityThreshold;
            this.PeakShapeThreshold = peakShapeThreshold;
            this.IsotopicThreshold = isotopicThreshold;
            this.MinFitPoints = minFitPoints;
            this.ExpectIsomer = expectIsomer;
            this.PeakDetectorSelection = peakDetectorSelection;
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
        public double DriftTimeToleranceInMs { get; private set; }

        /// <summary>
        /// Gets the peak detector selection.
        /// </summary>
        public PeakDetectorEnum PeakDetectorSelection { get; private set; }

        /// <summary>
        /// If ExpectIsomer is set to true. The algorithm will stop assuming there is one and only one
        /// target match in the expected Mz range. Instead if there are isomers the workflow will report 
        /// all isomers with reasonable socres.
        /// </summary>
        public bool ExpectIsomer{ get; private set; }
    }
}
