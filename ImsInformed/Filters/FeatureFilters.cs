﻿// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
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
            int scanImsRep = feature.PeakApex.DriftTimeCenterInScanNumber;

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
        public static bool FilterOnAbsoluteIntensity(StandardImsPeak feature, double intensityScore, double intensityThreshold = 0.5)
        {
            return intensityScore < intensityThreshold;
        }

        public static bool FilterOnRelativeIntesity(StandardImsPeak feature, double highestPeakIntensity, double percentage = 3)
        {
            if (percentage >= 100 || percentage < 0)
            {
                throw new ArgumentException(string.Format("Percentage argument of {0}% is invalid", percentage));
            }
            return feature.SummedIntensities < highestPeakIntensity / 100 * percentage;
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
            double massDifferenceInDalton = feature.PeakApex.MzCenterInDalton - target.MassWithAdduct;
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
