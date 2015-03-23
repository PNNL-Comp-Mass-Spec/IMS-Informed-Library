
namespace ImsInformed.Domain
{
    using System;

    public enum AnalysisStatus
    {
        Positive, // Result positive, ion found and mobility calculated.
        Negative, // Result negative, taget not found at the given UIMF file.
        Rejected, // Result rejected due to low analysis score.
        NotSufficientPoints, // No sufficient points for the fitline.
        UknownError, // Error
        TargetError, // Target construction problem
        NoAnalysis, // Analysis not scheduled
        ConflictRuns, // Conflict between experiments
        XicNotFound, // XIC is empty at the target MZ
        IsotopicProfileNotFound, // Target does not have an isotopic profile
        IsotopicProfileNotMatchTheoretical, 
        IsotopicProfileNotEnoughPeaks, // There are not enough peaks to compose isotopic profile.
        IsotopicFitScoreError, // Isotopic fit score is too low
        ElutionTimeError, 
        DriftTimeError,
        MassError,
        PeakToLeft, 
        ChargeStateCorrelation
    }

        /// <summary>
    /// The ionization method extensions.
    /// </summary>
    public static class AnalysisStatusUtilities
    {
        /// <summary>
        /// Convert the analysis status to three letter code that looks good when you print them in a text file.
        /// </summary>
        /// <param name="status">
        /// The ionization method.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static string ToConclusionCode(this AnalysisStatus status)
        {
            string method;

            if (status == AnalysisStatus.Positive)
            {
                method = "POS";
            }
            else if (status == AnalysisStatus.Negative)
            {
                method = "NEG";
            }
            else if (status == AnalysisStatus.Rejected)
            {
                method = "REJ";
            }
            else if (status == AnalysisStatus.NotSufficientPoints)
            {
                method = "NSP";
            }
            else if (status == AnalysisStatus.UknownError)
            {
                method = "ERR";
            }
            else if (status == AnalysisStatus.TargetError)
            {
                method = "TAR";
            }
            else if (status == AnalysisStatus.NoAnalysis)
            {
                method = "NAH";
            }
            else if (status == AnalysisStatus.ConflictRuns)
            {
                method = "CON";
            }
            else if (status == AnalysisStatus.XicNotFound)
            {
                method = "XIC";
            }
            else if (status == AnalysisStatus.IsotopicFitScoreError)
            {
                method = "ISO";
            }
            else if (status == AnalysisStatus.IsotopicProfileNotEnoughPeaks)
            {
                method = "ISO";
            }
            else if (status == AnalysisStatus.IsotopicProfileNotFound)
            {
                method = "ISO";
            }
            else if (status == AnalysisStatus.IsotopicProfileNotMatchTheoretical)
            {
                method = "ISO";
            }
            else if (status == AnalysisStatus.ElutionTimeError)
            {
                method = "ELU";
            }
            else if (status == AnalysisStatus.DriftTimeError)
            {
                method = "DFT";
            }
            else if (status == AnalysisStatus.MassError)
            {
                method = "MAS";
            }
            else if (status == AnalysisStatus.PeakToLeft)
            {
                method = "LFT";
            }
            else if (status == AnalysisStatus.ChargeStateCorrelation)
            {
                method = "COR";
            }
            else 
            {
                throw new ArgumentException("Ionization method [" + status + "] is not supported");
            }

            return method;
        }
    }
}
