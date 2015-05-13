// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObservedPeak.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the ObservedPeak type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DirectInjection
{
    using ImsInformed.Scoring;

    /// <summary>
    /// The observed peak.
    /// </summary>
    public class ObservedPeak
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservedPeak"/> class.
        /// </summary>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <param name="statistics">
        /// The Statistics.
        /// </param>
        public ObservedPeak(VoltageGroup group, StandardImsPeak peak, FeatureStatistics statistics)
        {
            this.VoltageGroup = group;
            this.Peak = peak;
            this.Statistics = statistics;
        }

        /// <summary>
        /// Gets the voltage group.
        /// </summary>
        public VoltageGroup VoltageGroup { get; private set; }

        /// <summary>
        /// Gets the voltage group.
        /// </summary>
        public FeatureStatistics Statistics { get; private set; }

        /// <summary>
        /// Gets or sets the cooks distance.
        /// </summary>
        public double CooksDistance { get; set; }

        /// <summary>
        /// Gets the peak.
        /// </summary>
        public StandardImsPeak Peak { get; private set; }

        /// <summary>
        /// Gets the observation description.
        /// </summary>
        public string ObservationDescription
        {
            get
            {
                double voltageGroupDescriptor = this.VoltageGroup.MeanVoltageInVolts;
                double peakDescriptor = this.Peak.HighestPeakApex.DriftTimeCenterInMs;
                double intensity = this.Peak.SummedIntensities;

                return string.Format("[{0:F2} V, {1:F2}, {2:F0}]", voltageGroupDescriptor, peakDescriptor, intensity);
            }
        }
    }
}
