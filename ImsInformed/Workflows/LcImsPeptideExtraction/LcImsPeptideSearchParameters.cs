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
namespace ImsInformed.Workflows.LcImsPeptideExtraction
{
    /// <summary>
    /// The informed parameters.
    /// </summary>
    public class LcImsPeptideSearchParameters
    {
        /// <summary>
        /// Gets or sets the net tolerance.
        /// </summary>
        public double NetTolerance { get; set; }

        /// <summary>
        /// Gets or sets the isotopic fit score max.
        /// </summary>
        public double IsotopicFitScoreThreshold { get; set; }

        /// <summary>
        /// Gets or sets the mass tolerance in ppm.
        /// </summary>
        public double MassToleranceInPpm { get; set; }

        /// <summary>
        /// Gets or sets the scan window width.
        /// </summary>
        public int ScanWindowWidth { get; set; }

        /// <summary>
        /// Gets or sets the charge state max.
        /// </summary>
        public int ChargeStateMax { get; set; }

        /// <summary>
        /// Gets or sets the number point for smoothing.
        /// </summary>
        public int NumPointForSmoothing { get; set; }
    }
}
