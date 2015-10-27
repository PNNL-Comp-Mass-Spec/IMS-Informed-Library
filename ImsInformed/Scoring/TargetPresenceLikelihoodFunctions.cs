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
    using System;

    using ImsInformed.Domain.DirectInjection;

    /// <summary>
    /// The likelihood function for if the feature is an actual ion instead of random noise.
    /// </summary>
    internal static class TargetPresenceLikelihoodFunctions
    {
        /// <summary>
        /// The intensity independent likelihood function. Better used if you have strong faith in the isotopic score 100% and/or your sample is really mixed and
        /// intensity is useless.
        /// </summary>
        /// <param name="observedPeak">
        /// The observed Peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityIndependentLikelihoodFunction(this ObservedPeak observedPeak)
        {
            PeakScores featureScores = observedPeak.Statistics;
            return featureScores.IsotopicScore;
        }

        /// <summary>
        /// The neutral likelihood function.
        /// </summary>
        /// <param name="observedPeak">
        /// The observed peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double NeutralLikelihoodFunction(this ObservedPeak observedPeak)
        {
            PeakScores featureScores = observedPeak.Statistics;
            return Math.Sqrt(featureScores.IntensityScore * featureScores.IsotopicScore);
        }

        /// <summary>
        /// The intensity dominant likelihood function. Say you have a pure sample, roll with this one might give better results.
        /// </summary>
        /// <param name="observedPeak">
        /// The observed Peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityDominantLikelihoodFunction(this ObservedPeak observedPeak)
        {
            PeakScores featureScores = observedPeak.Statistics;
            return featureScores.IntensityScore + 0.5 * featureScores.IsotopicScore;
        }
    }
}
