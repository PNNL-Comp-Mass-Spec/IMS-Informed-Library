// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScoreType.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the ScoreType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    /// <summary>
    /// The score type.
    /// </summary>
    public enum ScoreType
    {
        /// <summary>
        /// The intensity score. Help select real peaks.
        /// </summary>
        IntensityScore,

        /// <summary>
        /// The isotopic score. Help select right peaks.
        /// </summary>
        IsotopicScore,

        /// <summary>
        /// The peak shape score.Help select real peaks.
        /// </summary>
        PeakShapeScore,

        /// <summary>
        /// The voltage group stability score.
        /// </summary>
        VoltageGroupStabilityScore,

        /// <summary>
        /// The analysis score. Help evaluate confidence of scoring.
        /// </summary>
        AnalysisScore,
    }
}
