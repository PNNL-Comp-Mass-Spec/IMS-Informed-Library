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
namespace ImsInformed.Domain.LiquidChromatography
{
    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// The feature blob net.
    /// </summary>
    public class FeatureBlobNet
    {
        /// <summary>
        /// Gets the feature blob.
        /// </summary>
        public FeatureBlob FeatureBlob { get; private set; }

        /// <summary>
        /// Gets the normalized elution time.
        /// </summary>
        public double NormalizedElutionTime { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureBlobNet"/> class.
        /// </summary>
        /// <param name="featureBlob">
        /// The feature blob.
        /// </param>
        /// <param name="normalizedElutionTime">
        /// The normalized elution time.
        /// </param>
        public FeatureBlobNet(FeatureBlob featureBlob, double normalizedElutionTime)
        {
            this.FeatureBlob = featureBlob;
            this.NormalizedElutionTime = normalizedElutionTime;
        }
    }
}
