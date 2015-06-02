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
    using ImsInformed.Stats;

    internal enum ObservationType
    {
        Peak,
        Virtual
    }

    /// <summary>
    /// The observed peak.
    /// </summary>
    internal class ObservedPeak 
    {
        private ContinuousXYPoint mobilityPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservedPeak"/> class that is an virtual observation like source/sink.
        /// </summary>
        public ObservedPeak()
        {
            this.ObservationType = ObservationType.Virtual;
            this.Peak = null;
            this.Statistics = null;
            this.VoltageGroup = null;
            this.mobilityPoint = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservedPeak"/> class with peak and its calculated statistics.
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
            this.ObservationType = ObservationType.Peak;
            this.mobilityPoint = null;
        }

        /// <summary>
        /// Gets the observation type.
        /// </summary>
        public ObservationType ObservationType{ get; private set; }

        /// <summary>
        /// Gets the voltage group.
        /// </summary>
        public VoltageGroup VoltageGroup { get; private set; }

        /// <summary>
        /// Gets the voltage group.
        /// </summary>
        public FeatureStatistics Statistics { get; private set; }

        /// <summary>
        /// Gets the peak.
        /// </summary>
        public StandardImsPeak Peak { get; private set; }

        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            if (this.ObservationType == ObservationType.Peak)
            {
                double voltageGroupDescriptor = this.VoltageGroup.MeanVoltageInVolts;
                double peakDescriptor = this.Peak.HighestPeakApex.DriftTimeCenterInMs;
                double intensity = this.Peak.SummedIntensities;

                return string.Format("[{0:F2} V, {1:F2}, {2:F0}]", voltageGroupDescriptor, peakDescriptor, intensity);
            }
            else
            {
                return "Virtual";
            }
        }

        /// <summary>
        /// The to continuous xy point.
        /// </summary>
        /// <returns>
        /// The <see cref="ContinuousXYPoint"/>.
        /// </returns>
        public ContinuousXYPoint ToContinuousXyPoint()
        {
            if (this.mobilityPoint == null)
            {
                // convert drift time to SI unit seconds
                double x = this.Peak.HighestPeakApex.DriftTimeCenterInMs / 1000;
                
                // P/(T*V) value in pascal per (volts * kelvin)
                double y = this.VoltageGroup.MeanPressureNondimensionalized / 
                    this.VoltageGroup.MeanVoltageInVolts / 
                    this.VoltageGroup.MeanTemperatureNondimensionalized;
                 
                this.mobilityPoint = new ContinuousXYPoint(x, y);
            }

            return this.mobilityPoint;
        }
    }
}
