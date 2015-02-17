// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AnalysisScoresHolder.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The analysis scores holder.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    using System;

    /// <summary>
    /// The analysis scores holder.
    /// </summary>
    [Serializable]
    public struct AnalysisScoresHolder
    {
        /// <summary>
        /// The best feature score.
        /// </summary>
        public FeatureScoreHolder AverageCandidateTargetScores;

        /// <summary>
        /// The voltage group stability score.
        /// </summary>
        public double AverageVoltageGroupStabilityScore;

        /// <summary>
        /// The analysis score.
        /// </summary>
        public double RSquared;
    }
}
