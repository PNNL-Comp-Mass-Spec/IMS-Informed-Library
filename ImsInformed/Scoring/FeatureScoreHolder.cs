﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureScoreHolder.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the FeatureScoreHolder type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    using System;

    /// <summary>
    /// The feature score holder.
    /// </summary>
    [Serializable]
    public struct FeatureScoreHolder
    {
        /// <summary>
        /// The intensity score.
        /// </summary>
        public double IntensityScore;

        /// <summary>
        /// The isotopic score.
        /// </summary>
        public double IsotopicScore;

        /// <summary>
        /// The peak shape score.
        /// </summary>
        public double PeakShapeScore;
    }
}
