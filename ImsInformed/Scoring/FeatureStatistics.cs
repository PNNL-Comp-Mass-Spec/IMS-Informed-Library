﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureStatistics.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the FeatureStatistics type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    using System;

    /// <summary>
    /// The feature score holder.
    /// </summary>
    [Serializable]
    public class FeatureStatistics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureStatistics"/> class.
        /// </summary>
        /// <param name="intensityScore">
        /// The intensity score.
        /// </param>
        /// <param name="isotopicScore">
        /// The isotopic score.
        /// </param>
        /// <param name="peakShapeScore">
        /// The peak shape score.
        /// </param>
        public FeatureStatistics(double intensityScore, double isotopicScore, double peakShapeScore)
        {
            this.IntensityScore = intensityScore;
            this.PeakShapeScore = peakShapeScore;
            this.IsotopicScore = isotopicScore;
        }

        /// <summary>
        /// The intensity score.
        /// </summary>
        public readonly double IntensityScore;

        /// <summary>
        /// The isotopic score.
        /// </summary>
        public readonly double IsotopicScore;

        /// <summary>
        /// The peak shape score.
        /// </summary>
        public readonly double PeakShapeScore;
    }
}
