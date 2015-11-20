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
namespace ImsInformed.Domain.DataAssociation.IonSignatureMatching
{
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Util;

    /// <summary>
    /// The diffusion profile descriptor.
    /// </summary>
    internal class DiffusionProfileDescriptor
    {
        public DiffusionProfileDescriptor(ObservedPeak observation)
        {
            this.ArrivalTimeCenterLocation = observation.Peak.PeakCenterLocationOnArrivalTime();
            this.ArrivalTimeDiffusionWidthInMs = observation.Peak.PeakApex.DriftTimeFullWidthHalfMaxHigherBondInMs - observation.Peak.PeakApex.DriftTimeFullWidthHalfMaxLowerBondInMs;
            this.MzCenterLocation = observation.Peak.PeakCenterLocationOnMz();
            this.MzDiffusionWidthInPpm = Metrics.DaltonToPpm(observation.Peak.PeakApex.MzFullWidthHalfMaxHigh - observation.Peak.PeakApex.MzFullWidthHalfMaxLow, observation.Peak.PeakApex.MzCenterInDalton);
        }

        /// <summary>
        /// Gets the arrival time center location.
        /// </summary>
        public double ArrivalTimeCenterLocation { get; private set; }

        /// <summary>
        /// Gets the arrival time diffusion width.
        /// </summary>
        public double ArrivalTimeDiffusionWidthInMs { get; private set; }

        /// <summary>
        /// Gets the mz diffusion width.
        /// </summary>
        public double MzDiffusionWidthInPpm { get; private set; }

        /// <summary>
        /// Gets the mz center location.
        /// </summary>
        public double MzCenterLocation { get; private set; }
    }
}
