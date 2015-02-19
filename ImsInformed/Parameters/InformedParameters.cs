namespace ImsInformed.Parameters
{
    /// <summary>
    /// The informed parameters.
    /// </summary>
    public class InformedParameters
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
