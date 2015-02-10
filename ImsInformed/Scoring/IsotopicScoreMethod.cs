// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsotopicScoreMethod.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The isotopic score methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    /// <summary>
    /// The isotopic score methods.
    /// </summary>
    public enum IsotopicScoreMethod
    {
        /// <summary>
        /// The angle.
        /// </summary>
        Angle,

        /// <summary>
        /// The euclidean distance.
        /// </summary>
        EuclideanDistance,

        /// <summary>
        /// The pearson correlation.
        /// </summary>
        PearsonCorrelation,

        /// <summary>
        /// The bhattacharyya.
        /// </summary>
        Bhattacharyya,

        /// <summary>
        /// The euclidean distance alternative.
        /// </summary>
        EuclideanDistanceAlternative,
    }
}
