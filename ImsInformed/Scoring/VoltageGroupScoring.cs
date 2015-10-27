// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
namespace ImsInformed.Scoring
{
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;

    /// <summary>
    /// The voltage group score.
    /// </summary>
    internal class VoltageGroupScoring
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
