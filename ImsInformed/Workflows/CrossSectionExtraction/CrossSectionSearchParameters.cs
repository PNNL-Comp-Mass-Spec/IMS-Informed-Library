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
        public CrossSectionSearchParameters() : this(0.5, 250, 9, 0.25, 0.00, 0.4, 0.4, 3, false, PeakDetectorEnum.WaterShed, 0.9)
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
        public CrossSectionSearchParameters(double driftTimeToleranceInMs, double massToleranceInPpm, int numPointForSmoothing, double featureFilterLevel, double intensityThreshold, double peakShapeThreshold, double isotopicThreshold, int minFitPoints, bool expectIsomer, PeakDetectorEnum peakDetectorSelection, double minR2)
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
            this.minR2 = minR2;
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
        /// The min r 2.
        /// </summary>
        public readonly double minR2;

        /// <summary>
        /// If ExpectIsomer is set to true. The algorithm will stop assuming there is one and only one
        /// Target match in the expected centerMz range. Instead if there are isomers the workflow will report 
        /// all isomers with reasonable socres.
        /// </summary>
        public bool ExpectIsomer{ get; private set; }
    }
}
