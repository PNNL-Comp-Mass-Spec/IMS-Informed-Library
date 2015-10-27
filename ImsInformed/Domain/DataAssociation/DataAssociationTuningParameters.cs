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

    /// <summary>
    /// The ion association tunning class
    /// </summary>
    [Serializable]
    internal class DataAssociationTuningParameters
    {
        /// <summary>
        /// The intensity weight.
        /// </summary>
        public const double IntensityWeight = 2;

        /// <summary>
        /// The diffusion profile weight.
        /// </summary>
        public const double DiffusionProfileWeight = 1;

        /// <summary>
        /// The M/Z match weight.
        /// </summary>
        public const double MzMatchWeight = 3;

        /// <summary>
        /// The mz difference in ppm 09.
        /// </summary>
        public const double MzDifferenceInPpm09 = 30;

        /// <summary>
        /// The mz difference in ppm 09.
        /// </summary>
        public const double DriftTimeDifferenceInMs09 = 0.1;

        /// <summary>
        /// An outlier's Pr(xi | T)
        /// </summary>
        public const double PxTOutlier = 0.750;

        /// <summary>
        /// An inlier's Pr(xi | T)
        /// </summary>
        public const double PxTInlier = 1;
    }
}
