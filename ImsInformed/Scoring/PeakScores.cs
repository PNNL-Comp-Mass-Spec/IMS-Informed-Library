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

    /// <summary>
    /// The feature score holder.
    /// </summary>
    [Serializable]
    public class PeakScores
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PeakScores"/> class.
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
        public PeakScores(double intensityScore, double isotopicScore, double peakShapeScore)
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
