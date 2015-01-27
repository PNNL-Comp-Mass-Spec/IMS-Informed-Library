// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VoltageGroupScore.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the VoltageGroupScore type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    using ImsInformed.Domain;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// The voltage group score.
    /// </summary>
    public class VoltageGroupScore
    {
        /// <summary>
        /// A voltage group has stability higher score if the voltage group has more accumulations, less variations
        /// on voltage, temperature and pressure.
        /// </summary>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double VoltageGroupStabilityScore(VoltageGroup group)
        {
            return 0;
        }
    }
}
