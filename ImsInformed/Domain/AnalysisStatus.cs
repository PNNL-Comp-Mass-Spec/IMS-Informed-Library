﻿
namespace ImsInformed.Domain
{
    public enum AnalysisStatus
    {
        POS, // Result positive, ion found and mobility calculated.
        REJ, // Result rejected due to low analysis score.
        NSP, // No sufficient points for the fitline.
        ERR, // Error
        TAR, // Target construction problem
        NEG, // Result negative, ion not found
        NAH, // Analysis not scheduled
        CON, // Conflict between experiments
        XicNotFound,
        IsotopicProfileNotFound,
        IsotopicProfileNotMatchTheoretical,
        IsotopicProfileNotEnoughPeaks,
        IsotopicFitScoreError,
        ElutionTimeError,
        DriftTimeError,
        MassError,
        PeakToLeft, 
        ChargeStateCorrelation
    }
}
