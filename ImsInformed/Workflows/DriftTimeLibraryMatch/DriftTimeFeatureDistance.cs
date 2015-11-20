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
    using System;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Targets;
    using ImsInformed.Util;

    /// <summary>
    /// The drift time matching util.
    /// </summary>
    public class DriftTimeFeatureDistance : IComparable<DriftTimeFeatureDistance>
    {
        public DriftTimeFeatureDistance(DriftTimeTarget target, StandardImsPeak observedPeak, VoltageGroup voltageGroupThePeakWasObserved)
        {
            double predicatedDriftTime = IMSUtil.DeNormalizeDriftTime(target.NormalizedDriftTimeInMs, voltageGroupThePeakWasObserved);
            this.DriftTimeDifferenceInMs = observedPeak.PeakApex.DriftTimeCenterInMs - predicatedDriftTime;
            this.MassDifferenceInDalton = observedPeak.PeakApex.MzCenterInDalton - target.MassWithAdduct;
            this.MassDifferenceInPpm = this.MassDifferenceInDalton / target.MassWithAdduct * 1000000;
        }

        /// <summary>
        /// Gets the mass difference in dalton.
        /// </summary>
        public double MassDifferenceInDalton { get; private set; }

        /// <summary>
        /// Gets the mass difference in dalton.
        /// </summary>
        public double MassDifferenceInPpm { get; private set; }

        /// <summary>
        /// Gets the drift time difference in milliseconds.
        /// </summary>
        public double DriftTimeDifferenceInMs { get; private set; }

        public int CompareTo(DriftTimeFeatureDistance other)
        {
            return SSD(this).CompareTo(SSD(other));
        }

        private static double SSD(DriftTimeFeatureDistance distance)
        {
            return Math.Sqrt(Math.Abs(distance.DriftTimeDifferenceInMs * distance.MassDifferenceInDalton));
        }
    }
}
