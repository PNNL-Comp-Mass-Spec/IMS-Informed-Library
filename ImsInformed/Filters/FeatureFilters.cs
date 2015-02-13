// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureFilters.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the FeatureFilters type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Filters
{
    using System;

    using ImsInformed.Scoring;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// The feature filters.
    /// </summary>
    public class FeatureFilters
    {
        /// <summary>
        /// The filter extreme drift time.
        /// </summary>
        /// <param name="feature">
        /// The feature.
        /// </param>
        /// <param name="totalImsScans">
        /// The total ims scans.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool FilterExtremeDriftTime(FeatureBlob feature, int totalImsScans)
        {
            int scanImsRep = feature.Statistics.ScanImsRep;

            // Nullify the intensity score if the Scan is in 1% scans left or right areas.
            int errorMargin = (int)Math.Round(totalImsScans * 0.01);
            return scanImsRep < errorMargin || scanImsRep > totalImsScans - errorMargin;
        }

        /// <summary>
        /// The filter low intensity.
        /// </summary>
        /// <param name="feature">
        /// The feature.
        /// </param>
        /// <param name="intensityScore">
        /// The intensity score.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool FilterLowIntensity(FeatureBlob feature, double intensityScore)
        {
            return intensityScore < 0.5;
        }

        public static bool FilterBadPeakShape(FeatureBlob feature, double peakShapeScore)
        {
            return peakShapeScore < 0.4;
        }


        /// <summary>
        /// The filter bad isotopic profile.
        /// </summary>
        /// <param name="feature">
        /// The feature.
        /// </param>
        /// <param name="totalImsScans">
        /// The total ims scans.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool FilterBadIsotopicProfile(FeatureBlob feature, double isotopicScore)
        {
            return isotopicScore < 0.4;
        }
    }
}
