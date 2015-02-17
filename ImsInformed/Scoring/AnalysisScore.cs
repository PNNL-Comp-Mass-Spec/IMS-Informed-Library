// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RSquared.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the RSquared type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    /// <summary>
    /// The analysis score. Used to quantize how confident the analysis is.
    /// </summary>
    public class AnalysisScore
    {
        /// <summary>
        /// The compute analysis score.
        /// </summary>
        /// <param name="rSquared">
        /// The r Squared.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double ComputeAnalysisScore(double rSquared)
        {
            return rSquared;
        }
    }
}
