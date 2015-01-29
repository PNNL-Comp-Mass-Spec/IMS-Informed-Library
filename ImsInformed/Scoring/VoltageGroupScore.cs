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

    using ImsInformed.Domain;

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
        public static double ComoputeVoltageGroupStabilityScore(VoltageGroup group)
        {
            double stability = group.VariancePressure * group.VarianceTemperature * group.VarianceVoltage;
            return ScoreUtil.MapToZeroOne(stability, true, 0.00000000000000001);
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
        public static double AverageVoltageGroupStabilityScore(IEnumerable<VoltageGroup> groups)
        {
            int count = 0;
            double averageStabilityScore;
            averageStabilityScore = 0;

            foreach (var group in groups)
            {
                count++;
                averageStabilityScore += ComoputeVoltageGroupStabilityScore(group);
            }

            averageStabilityScore /= count;
            return averageStabilityScore;
        }
    }
}
