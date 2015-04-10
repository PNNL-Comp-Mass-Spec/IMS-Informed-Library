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
