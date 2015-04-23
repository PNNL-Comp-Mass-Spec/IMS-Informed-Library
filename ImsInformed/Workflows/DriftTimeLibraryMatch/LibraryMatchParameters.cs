// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LcImsPeptideSearchParameters.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The informed parameters.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows.DriftTimeLibraryMatch
{
    /// <summary>
    /// The informed parameters.
    /// </summary>
    public class LibraryMatchParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryMatchParameters"/> class.
        /// </summary>
        public LibraryMatchParameters() : this(1, 250, 9, 0.4, 0.4, 0.25, 25)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryMatchParameters"/> class.
        /// </summary>
        /// <param name="driftTimeToleranceInMs">
        /// The drift time tolerance in ms.
        /// </param>
        /// <param name="initialSearchMassToleranceInPpm">
        /// The mass tolerance in ppm.
        /// </param>
        /// <param name="numPointForSmoothing">
        /// The num point for smoothing.
        /// </param>
        /// <param name="peakShapeThreshold">
        /// The peak shape threshold.
        /// </param>
        /// <param name="isotopicThreshold">
        /// The isotopic threshold.
        /// </param>
        /// <param name="featureFilterLevel">
        /// The feature Filter Level.
        /// </param>
        /// <param name="matchingMassToleranceInPpm">
        /// The matching Mass Tolerance In Ppm.
        /// </param>
        public LibraryMatchParameters(double driftTimeToleranceInMs, double initialSearchMassToleranceInPpm, int numPointForSmoothing, double peakShapeThreshold, double isotopicThreshold, double featureFilterLevel, double matchingMassToleranceInPpm)
        {
            this.DriftTimeToleranceInMs = driftTimeToleranceInMs;
            this.InitialSearchMassToleranceInPpm = initialSearchMassToleranceInPpm;
            this.NumPointForSmoothing = numPointForSmoothing;
            this.PeakShapeThreshold = peakShapeThreshold;
            this.IsotopicThreshold = isotopicThreshold;
            this.FeatureFilterLevel = featureFilterLevel;
            this.MatchingMassToleranceInPpm = matchingMassToleranceInPpm;
        }

        /// <summary>
        /// Gets or sets the drift time tolerance in milliseconds.
        /// </summary>
        public double DriftTimeToleranceInMs { get; private set;  }

        /// <summary>
        /// Gets or sets the mass tolerance in ppm for window the target initially.
        /// </summary>
        public double InitialSearchMassToleranceInPpm { get; private set; }

        /// <summary>
        /// Gets or sets the matching mass tolerance in ppm, that is, how far away the peak center can be away from the target Mz to  be still considered a match 
        /// </summary>
        public double MatchingMassToleranceInPpm { get; set; }

        /// <summary>
        /// Gets or sets the number point for smoothing.
        /// </summary>
        public int NumPointForSmoothing { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double PeakShapeThreshold { get; private set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double IsotopicThreshold { get; private set; }

        /// <summary>
        /// Gets or sets the feature filter level.
        /// </summary>
        public double FeatureFilterLevel { get; private set; }
    }
}
