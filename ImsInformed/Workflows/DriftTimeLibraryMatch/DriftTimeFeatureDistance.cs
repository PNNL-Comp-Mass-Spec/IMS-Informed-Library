// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DriftTimeFeatureDistance.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the DriftTimeFeatureDistance type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

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
            this.DriftTimeDifferenceInMs = observedPeak.HighestPeakApex.DriftTimeCenterInMs - predicatedDriftTime;
            this.MassDifferenceInDalton = observedPeak.HighestPeakApex.MzCenterInDalton - target.MassWithAdduct;
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
