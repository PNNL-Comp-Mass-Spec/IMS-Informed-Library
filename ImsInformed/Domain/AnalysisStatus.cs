
namespace ImsInformed.Domain
{
    public enum AnalysisStatus
    {
        POS, // Result positive, ion found and mobility calculated.
        ERR, // Error
        NEG, // Result negative, ion not found
        Nah, // Analysis not scheduled
        XicNotFound,
        IsotopicProfileNotFound,
        IsotopicProfileNotMatchTheoretical,
        IsotopicProfileNotEnoughPeaks,
        IsotopicFitScoreError,
        ElutionTimeError,
        DriftTimeError,
        MassError,
        PeakToLeft,
        NSP, // No sufficient points for the fitline.
        ChargeStateCorrelation
    }
}
