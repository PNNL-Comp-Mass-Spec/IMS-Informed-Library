// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StandardImsPeak.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The unified IMS peak class that provides a unified peak/feature representation for numerous different peak/feature detectors.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    using System;

    using MagnitudeConcavityPeakFinder;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// The unified IMS peak class that provides a unified peak/feature representation for numerous different peak/feature detectors.
    /// </summary>
    [Serializable]
    public class StandardImsPeak
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandardImsPeak"/> class from the feature class FeatureBlob used in Multidimensional Peak Finder.
        /// </summary>
        /// <param name="WatershedFeature">
        /// The watershed feature.
        /// </param>
        public StandardImsPeak(FeatureBlob WatershedFeature)
        {
        
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardImsPeak"/> class from the feature class clsPeak used in Magnitude Concavity Peak Finder.
        /// </summary>
        /// <param name="magnitudeConcavityPeakFinder">
        /// The magnitude concavity peak finder.
        /// </param>
        public StandardImsPeak(clsPeak magnitudeConcavityPeakFinder)
        {
        
        }

        public int DriftTimeCenterInScanNumber { get; private set; }

        public double DriftTimeCenterInMs { get; private set; }

        public int MzCenterInBinNumber { get; private set; }

        public double MzCenterInDalton { get; private set; }
    }
}
