// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureLikelihoodFunctions.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the FeatureLikelihoodFunctions type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    using System;

    using ImsInformed.Domain.DirectInjection;

    /// <summary>
    /// The likelihood function for if the feature is an actual ion instead of random noise.
    /// </summary>
    internal static class TargetPresenceLikelihoodFunctions
    {
        /// <summary>
        /// The intensity independent likelihood function. Better used if you have strong faith in the isotopic score 100% and/or your sample is really mixed and
        /// intensity is useless.
        /// </summary>
        /// <param name="observedPeak">
        /// The observed Peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityIndependentLikelihoodFunction(this ObservedPeak observedPeak)
        {
            PeakScores featureScores = observedPeak.Statistics;
            return featureScores.IsotopicScore;
        }

        /// <summary>
        /// The neutral likelihood function.
        /// </summary>
        /// <param name="observedPeak">
        /// The observed peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double NeutralLikelihoodFunction(this ObservedPeak observedPeak)
        {
            PeakScores featureScores = observedPeak.Statistics;
            return Math.Sqrt(featureScores.IntensityScore * featureScores.IsotopicScore);
        }

        /// <summary>
        /// The intensity dominant likelihood function. Say you have a pure sample, roll with this one might give better results.
        /// </summary>
        /// <param name="observedPeak">
        /// The observed Peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityDominantLikelihoodFunction(this ObservedPeak observedPeak)
        {
            PeakScores featureScores = observedPeak.Statistics;
            return featureScores.IntensityScore + 0.5 * featureScores.IsotopicScore;
        }
    }
}
