﻿// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
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
namespace ImsInformed
{
    using System;

    /// <summary>
    /// The analysis status.
    /// </summary>
    public enum AnalysisStatus
    {
        /// <summary>
        /// Result positive, depending on workflows used this have different meanings.
        /// </summary>
        Positive,

        /// <summary>
        /// Result negative, depending on workflows used this have different meanings.
        /// </summary>
        Negative,

        /// <summary>
        /// Result rejected, depending on workflows used this have different meanings.
        /// </summary>
        Rejected,

        /// <summary>
        /// No sufficient points for the fit FitLine.
        /// </summary>
        NotSufficientPoints, 

        /// <summary>
        /// Unknown error.
        /// </summary>
        UknownError,

        /// <summary>
        /// Errors occurred in the Target construction process
        /// </summary>
        TargetError, 

        /// <summary>
        /// No analysis was performed on this Target.
        /// </summary>
        NoAnalysis, 

        /// <summary>
        /// Replicates of the same experiment give conflicting analysis results.
        /// </summary>
        ConflictRuns, // Conflict between experiments

        /// <summary>
        /// XIC is empty at the Target MZ
        /// </summary>
        XicNotFound,

        /// <summary>
        /// Target does not have an isotopic profile
        /// </summary>
        IsotopicProfileNotFound, 

        /// <summary>
        /// The isotopic profile not match theoretical.
        /// </summary>
        IsotopicProfileNotMatchTheoretical,

        /// <summary>
        /// There are not enough peaks to compose isotopic profile.
        /// </summary>
        IsotopicProfileNotEnoughPeaks,

        /// <summary>
        /// Isotopic fit score is below threshold
        /// </summary>
        IsotopicFitScoreError, 

        /// <summary>
        /// The elution time error.
        /// </summary>
        ElutionTimeError,

        /// <summary>
        /// The drift time error.
        /// </summary>
        DriftTimeError,

        /// <summary>
        /// The mass error.
        /// </summary>
        MassError,

        /// <summary>
        /// The peak to left.
        /// </summary>
        PeakToLeft,

        /// <summary>
        /// The charge state correlation.
        /// </summary>
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
