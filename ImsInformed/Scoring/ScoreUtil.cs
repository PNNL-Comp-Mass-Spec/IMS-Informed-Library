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

    /// <summary>
    /// The score util.
    /// </summary>
    public class ScoreUtil
    {
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
        /// The x value to map to 0.9. Used to define a "good" score.
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
                // Solve (Pi/2 - atan(x)) / (PI/2) = 0.9
                scale = Math.Tan(0.1 * Math.PI / 2) / ninetyPercentX;
            }
            else
            {
                // Solve atan(x) / (PI/2) = 0.9
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
