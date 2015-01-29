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
    public class FeatureLikelihoodFunctions
    {
        /// <summary>
        /// The intensity only likelyhood function.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityOnlyLikelyhoodFunction(FeatureScoreHolder featureScores)
        {
            return featureScores.IntensityScore;
        }

        /// <summary>
        /// The intensity independent likelyhood function.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityIndependentLikelyhoodFunction(FeatureScoreHolder featureScores)
        {
            return featureScores.IsotopicScore* 3 + featureScores.PeakShapeScore;
        }

        /// <summary>
        /// The intensity dependent likelyhood function.
        /// </summary>
        /// <param name="featureScores">
        /// The feature scores.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityDependentLikelyhoodFunction(FeatureScoreHolder featureScores)
        {
            if (featureScores.IsotopicScore != 0)
            {
                return featureScores.IntensityScore * featureScores.IsotopicScore * featureScores.PeakShapeScore;
            } 
            else 
            {
                return featureScores.IntensityScore * featureScores.PeakShapeScore; 
            }
        }
    }
}
