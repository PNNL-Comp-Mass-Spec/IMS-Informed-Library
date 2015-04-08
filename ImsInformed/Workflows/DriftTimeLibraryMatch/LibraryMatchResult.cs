namespace ImsInformed.Workflows.DriftTimeLibraryMatch
{
    using ImsInformed.Domain;

    /// <summary>
    /// The library match result.
    /// </summary>
    public class LibraryMatchResult
    {
        /// <summary>
        /// Gets the ims feature.
        /// </summary>
        public StandardImsPeak ImsFeature { get; private set; }

        /// <summary>
        /// Gets the analysis status.
        /// </summary>
        public AnalysisStatus AnalysisStatus { get; private set; }
    }
}
