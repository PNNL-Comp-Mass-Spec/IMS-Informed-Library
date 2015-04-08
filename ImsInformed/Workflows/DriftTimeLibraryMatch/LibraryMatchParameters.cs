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
        public LibraryMatchParameters() : this(1, 10, 9, 0.4, 0.4, 0.25)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryMatchParameters"/> class.
        /// </summary>
        /// <param name="driftTimeToleranceInMs">
        /// The drift time tolerance in ms.
        /// </param>
        /// <param name="massToleranceInPpm">
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
        public LibraryMatchParameters(double driftTimeToleranceInMs, double massToleranceInPpm, int numPointForSmoothing, double peakShapeThreshold, double isotopicThreshold, double featureFilterLevel)
        {
            this.DriftTimeToleranceInMs = driftTimeToleranceInMs;
            this.MassToleranceInPpm = massToleranceInPpm;
            this.NumPointForSmoothing = numPointForSmoothing;
            this.PeakShapeThreshold = peakShapeThreshold;
            this.IsotopicThreshold = isotopicThreshold;
            this.FeatureFilterLevel = featureFilterLevel;
        }

        /// <summary>
        /// Gets or sets the drift time tolerance in milliseconds.
        /// </summary>
        public double DriftTimeToleranceInMs { get; private set;  }

        /// <summary>
        /// Gets or sets the mass tolerance in ppm.
        /// </summary>
        public double MassToleranceInPpm { get; private set; }

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
