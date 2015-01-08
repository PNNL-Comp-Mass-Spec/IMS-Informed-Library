
namespace ImsInformed.Domain
{
    public enum AnalysisStatus
    {
        Positive,
        Error,
        Negative,
        XicNotFound,
        IsotopicProfileNotFound,
        IsotopicProfileNotMatchTheoretical,
        IsotopicProfileNotEnoughPeaks,
        IsotopicFitScoreError,
        ElutionTimeError,
        DriftTimeError,
        MassError,
        PeakToLeft,
        NotSufficientPointsForFitline,
        ChargeStateCorrelation
    }
}
