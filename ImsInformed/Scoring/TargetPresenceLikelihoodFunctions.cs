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
        /// The intensity only likelihood function.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityOnlyLikelihoodFunction(FeatureScoreHolder featureScores)
        {
            return featureScores.IntensityScore;
        }

        /// <summary>
        /// The intensity independent likelihood function.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityIndependentLikelihoodFunction(FeatureScoreHolder featureScores)
        {
            return featureScores.IsotopicScore;
        }

        /// <summary>
        /// The intensity dependent likelihood function.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityDependentLikelihoodFunction(FeatureScoreHolder featureScores)
        {
            return featureScores.IntensityScore + 2 * featureScores.IsotopicScore;
        }

        /// <summary>
        /// The intensity dominant likelihood function.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityDominantLikelihoodFunction(FeatureScoreHolder featureScores)
        {
            return featureScores.IntensityScore + 0.5 * featureScores.IsotopicScore;
        }
    }
}
