// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DiffusionProfileDescriptor.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the DiffusionProfileDescriptor type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

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
            this.ArrivalTimeDiffusionWidthInMs = observation.Peak.HighestPeakApex.DriftTimeFullWidthHalfMaxHigherBondInMs - observation.Peak.HighestPeakApex.DriftTimeFullWidthHalfMaxLowerBondInMs;
            this.MzCenterLocation = observation.Peak.PeakCenterLocationOnMz();
            this.MzDiffusionWidthInPpm = Metrics.DaltonToPpm(observation.Peak.HighestPeakApex.MzFullWidthHalfMaxHigh - observation.Peak.HighestPeakApex.MzFullWidthHalfMaxLow, observation.Peak.HighestPeakApex.MzCenterInDalton);
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
