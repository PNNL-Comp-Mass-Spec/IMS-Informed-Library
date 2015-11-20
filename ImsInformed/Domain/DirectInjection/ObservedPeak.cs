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
namespace ImsInformed.Domain.DirectInjection
{
    using ImsInformed.Scoring;
    using ImsInformed.Statistics;

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
        public ObservedPeak(VoltageGroup group, StandardImsPeak peak, PeakScores statistics)
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
        public PeakScores Statistics { get; private set; }

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
                double peakDescriptor = this.Peak.PeakApex.DriftTimeCenterInMs;
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
                double y = this.Peak.PeakApex.DriftTimeCenterInMs;
                
                // P/(T*V) value in pascal per (volts * kelvin)
                double x = this.VoltageGroup.MeanPressureNondimensionalized / 
                    this.VoltageGroup.MeanVoltageInVolts / 
                    this.VoltageGroup.MeanTemperatureNondimensionalized;
                 
                this.mobilityPoint = new ContinuousXYPoint(x, y);
            }

            return this.mobilityPoint;
        }
    }
}
