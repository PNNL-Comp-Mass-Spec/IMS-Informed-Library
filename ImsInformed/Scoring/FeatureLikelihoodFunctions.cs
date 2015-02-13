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
    /// <summary>
    /// The feature likelihood functions.
    /// </summary>
    public class FeatureLikelihoodFunctions
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
            if (featureScores.IsotopicScore != 0)
            {
                return 2 * featureScores.IntensityScore + featureScores.IsotopicScore;
            } 
            else 
            {
                return featureScores.IntensityScore; 
            }
        }
    }
}
