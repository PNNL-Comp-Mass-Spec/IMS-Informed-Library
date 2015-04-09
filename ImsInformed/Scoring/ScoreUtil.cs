// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScoreUtil.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the ScoreUtil type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// The score util.
    /// </summary>
    public class ScoreUtil
    {
        /// <summary>
        /// The compare features function.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        public delegate double LikelihoodFunc(FeatureScoreHolder featureScores);

        /// <summary>
        /// The compare feature score.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <param name="likelihoodFunc">
        /// The likelihood Func.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int CompareFeatureScore(FeatureScoreHolder a, FeatureScoreHolder b, LikelihoodFunc likelihoodFunc)
        {
            return likelihoodFunc(a).CompareTo(likelihoodFunc(b));
        }

        /// <summary>
        /// The more likely than.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <param name="likelihoodFunc">
        /// The likelyhood func.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool MoreLikelyThan(FeatureScoreHolder a, FeatureScoreHolder b, LikelihoodFunc likelihoodFunc)
        {
            int result = CompareFeatureScore(a, b, likelihoodFunc);
            return result > 0;
        }

        /// <summary>
        /// The select most likely feature.
        /// </summary>
        /// <param name="scores">
        /// The scores.
        /// </param>
        /// <param name="likelihoodFunc">
        /// The likelihood func.
        /// </param>
        /// <returns>
        /// The <see cref="FeatureBlob"/>.
        /// </returns>
        public static StandardImsPeak SelectMostLikelyFeature(IDictionary<StandardImsPeak, FeatureScoreHolder> scores, LikelihoodFunc likelihoodFunc)
        {
            // Select the feature with the highest isotopic score
            StandardImsPeak bestFeature = null;
            FeatureScoreHolder mostLikelyPeakScores;
            mostLikelyPeakScores.IntensityScore = 0;
            mostLikelyPeakScores.IsotopicScore = 0;
            mostLikelyPeakScores.PeakShapeScore = 0;

            foreach (var featureBlob in scores.Keys)
            {
                FeatureScoreHolder currentScoreHolder = scores[featureBlob];
            
                // Evaluate feature scores.
                if (MoreLikelyThan(currentScoreHolder, mostLikelyPeakScores, likelihoodFunc))
                {
                    bestFeature = featureBlob;
                    mostLikelyPeakScores = currentScoreHolder;
                }
            }

            return bestFeature;
        }

        /// <summary>
        /// The map to zero one.
        /// </summary>
        /// <param name="score">
        /// The score.
        /// </param>
        /// <param name="inverseMapping">
        /// The inverse mapping.
        /// </param>
        /// <param name="ninetyPercentX">
        /// The X value to map to 0.9. Used to define a "good" score.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double MapToZeroOne(double score, bool inverseMapping, double ninetyPercentX)
        {
            double scale = 0;
            
            // Compute the scale from ninetyPercentX

            if (inverseMapping)
            {
                // Solve (Pi/2 - atan(X)) / (PI/2) = 0.9
                scale = Math.Tan(0.1 * Math.PI / 2) / ninetyPercentX;
            }
            else
            {
                // Solve atan(X) / (PI/2) = 0.9
                scale = Math.Tan(0.9 * Math.PI / 2) / ninetyPercentX;
            }

            double normalizedScore = score * scale;
            if (inverseMapping)
            {
                normalizedScore = Math.PI / 2 - Math.Atan(normalizedScore);
            }
            else
            {
                normalizedScore = Math.Atan(normalizedScore);
            }
            normalizedScore /= Math.PI / 2;
            return normalizedScore;
        }
    }
}
