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
namespace ImsInformed.Workflows.DriftTimeLibraryMatch
{
    using ImsInformed.Domain;

    /// <summary>
    /// The library match result.
    /// </summary>
    public class LibraryMatchResult
    {
        /// <summary>
        /// The distance.
        /// </summary>
        private readonly DriftTimeFeatureDistance distance;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryMatchResult"/> class.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <param name="conlusion">
        /// The conlusion.
        /// </param>
        /// <param name="distance">
        /// The distance.
        /// </param>
        public LibraryMatchResult(StandardImsPeak peak, AnalysisStatus conlusion, DriftTimeFeatureDistance distance)
        { 
            this.AnalysisStatus = conlusion;
            this.ImsFeature = peak;
            this.distance = distance;
        }

        /// <summary>
        /// Gets the ims feature.
        /// </summary>
        public StandardImsPeak ImsFeature { get; private set; }

        /// <summary>
        /// Gets the analysis status.
        /// </summary>
        public AnalysisStatus AnalysisStatus { get; private set; }

        /// <summary>
        /// Gets the mass difference in dalton.
        /// </summary>
        public double MassDifferenceInDalton
        {
            get
            {
                return this.distance.MassDifferenceInDalton;
            }
        }

        /// <summary>
        /// Gets the drift time difference in milliseconds.
        /// </summary>
        public double DriftTimeDifferenceInMs
        {
            get
            {
                return this.distance.DriftTimeDifferenceInMs;
            }
        }
    }
}
