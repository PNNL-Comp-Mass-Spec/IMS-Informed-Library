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

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Targets;

    /// <summary>
    /// The feature filters.
    /// </summary>
    internal class FeatureFilters
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
        public static bool FilterExtremeDriftTime(StandardImsPeak feature, int totalImsScans)
        {
            int scanImsRep = feature.HighestPeakApex.DriftTimeCenterInScanNumber;

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
        public static bool FilterLowIntensity(StandardImsPeak feature, double intensityScore, double intensityThreshold = 0.5)
        {
            return intensityScore < intensityThreshold;
        }

        /// <summary>
        /// The filter bad peak shape.
        /// </summary>
        /// <param name="feature">
        /// The feature.
        /// </param>
        /// <param name="peakShapeScore">
        /// The peak shape score.
        /// </param>
        /// <param name="peakShapeThreshold">
        /// The peak shape t hreshold.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool FilterBadPeakShape(StandardImsPeak feature, double peakShapeScore, double peakShapeThreshold = 0.4)
        {
            return peakShapeScore < peakShapeThreshold;
        }

        /// <summary>
        /// The filter high mz distance.
        /// </summary>
        /// <param name="feature">
        /// The feature.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="matchingMassToleranceInPpm">
        /// The matching mass tolerance in ppm.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool FilterHighMzDistance(StandardImsPeak feature, DriftTimeTarget target, double matchingMassToleranceInPpm)
        {
            double massDifferenceInDalton = feature.HighestPeakApex.MzCenterInDalton - target.MassWithAdduct;
            double massDifferenceInPpm = massDifferenceInDalton / target.MassWithAdduct * 1000000;
            return Math.Abs(massDifferenceInPpm) > matchingMassToleranceInPpm;
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
        public static bool FilterBadIsotopicProfile(StandardImsPeak feature, double isotopicScore, double isotopicThreshold = 0.4)
        {
            return isotopicScore < isotopicThreshold;
        }
    }
}
