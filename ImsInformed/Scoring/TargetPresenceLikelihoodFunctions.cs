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

    /// <summary>
    /// The feature likelihood functions.
    /// </summary>
    public class TargetPresenceLikelihoodFunctions
    {
        /// <summary>
        /// The intensity only likelihood function. Better used if your Target/feature match is more dependent on intensity then anything else.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityOnlyLikelihoodFunction(FeatureStatistics featureScores)
        {
            return featureScores.IntensityScore;
        }

        /// <summary>
        /// The intensity independent likelihood function. Better used if you have strong faith in the isotopic score 100% and/or your sample is really mixed and
        /// intensity is useless.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityIndependentLikelihoodFunction(FeatureStatistics featureScores)
        {
            return featureScores.IsotopicScore;
        }

        /// <summary>
        /// The intensity dependent likelihood function. Better used if you have good faith in the isotopic score 75% and/or your sample is really mixed intensity is 
        /// no longer a good indicator of feature/Target match.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IsotopicScoreDominantLikelihoodFunction(FeatureStatistics featureScores)
        {
            return featureScores.IntensityScore + 2 * featureScores.IsotopicScore;
        }

        /// <summary>
        /// The neutral likelihood function. Better used if you are not sure what your Target/feature match depends on
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double NeutralLikelihoodFunction(FeatureStatistics featureScores)
        {
            return featureScores.IntensityScore + featureScores.IsotopicScore;
        }

        /// <summary>
        /// The intensity dominant likelihood function. Say you have a pure sample, roll with this one might give better results.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityDominantLikelihoodFunction(FeatureStatistics featureScores)
        {
            return featureScores.IntensityScore + 0.5 * featureScores.IsotopicScore;
        }
    }
}
