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
namespace ImsInformed.Domain.DataAssociation
{
    using System;

    using ImsInformed.Scoring;

    /// <summary>
    /// Information on merits of the optimal assiciation
    /// </summary>
    [Serializable]
    public class AssociationHypothesisInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssociationHypothesisInfo"/> class.
        /// </summary>
        /// <param name="probabilityOfDataGivenHypothesis">
        /// The probability of data given hypothesis.
        /// </param>
        /// <param name="probabilityOfHypothesisGivenData">
        /// The probability of hypothesis given data.
        /// </param>
        public AssociationHypothesisInfo(double probabilityOfDataGivenHypothesis, double probabilityOfHypothesisGivenData)
        {
            this.ProbabilityOfDataGivenHypothesis = probabilityOfDataGivenHypothesis;
            this.ProbabilityOfHypothesisGivenData = probabilityOfHypothesisGivenData;
        }

        /// <summary>
        /// The probability of data given hypothesis.
        /// </summary>
        public readonly double ProbabilityOfDataGivenHypothesis;

        /// <summary>
        /// The probability of hypothesis given data.
        /// </summary>
        public readonly double ProbabilityOfHypothesisGivenData;
    }
}
