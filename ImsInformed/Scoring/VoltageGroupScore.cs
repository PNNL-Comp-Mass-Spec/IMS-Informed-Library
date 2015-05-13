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
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;

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
        public static double ComputeVoltageGroupStabilityScore(VoltageGroup group)
        {
            double stability = group.VariancePressureNondimensionalized * group.VarianceTemperature * group.VarianceVoltage;
            return ScoreUtil.MapToZeroOneTrignometry(stability, true, 0.00000000000000001);
        }

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
        public static double ComputeAverageVoltageGroupStabilityScore(IEnumerable<VoltageGroup> groups)
        {
            int count = 0;
            double averageStabilityScore;
            averageStabilityScore = 0;

            foreach (var group in groups)
            {
                count++;
                averageStabilityScore += ComputeVoltageGroupStabilityScore(group);
            }

            if (count == 0)
            {
                return 0;
            }

            averageStabilityScore /= count;
            return averageStabilityScore;
        }
    }
}
