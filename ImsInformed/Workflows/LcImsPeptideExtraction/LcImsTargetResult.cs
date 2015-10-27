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
namespace ImsInformed.Workflows.LcImsPeptideExtraction
{
    using DeconTools.Backend;
    using DeconTools.Backend.Core;

    using ImsInformed.Domain;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// The IMS Target result.
    /// </summary>
    public class LcImsTargetResult
    {
        /// <summary>
        /// Gets or sets the charge state.
        /// </summary>
        public int ChargeState { get; set; }

        /// <summary>
        /// Gets or sets the feature blob statistics.
        /// </summary>
        public FeatureBlobStatistics FeatureBlobStatistics { get; set; }

        /// <summary>
        /// Gets or sets the scan lc rep.
        /// </summary>
        public int ScanLcRep { get; set; }

        /// <summary>
        /// Gets or sets the normalized elution time.
        /// </summary>
        public double NormalizedElutionTime { get; set; }

        /// <summary>
        /// Gets or sets the drift time.
        /// </summary>
        public double DriftTime { get; set; }

        /// <summary>
        /// Gets or sets the monoisotopic mass.
        /// </summary>
        public double MonoisotopicMass { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is saturated.
        /// </summary>
        public bool IsSaturated { get; set; }

        /// <summary>
        /// Gets or sets the isotopic profile.
        /// </summary>
        public IsotopicProfile IsotopicProfile { get; set; }

        /// <summary>
        /// Gets or sets the isotoic fit score.
        /// </summary>
        public double IsotoicFitScore { get; set; }

        /// <summary>
        /// Gets or sets the ppm error.
        /// </summary>
        public double PpmError { get; set; }

        /// <summary>
        /// Gets or sets the mass spectrum.
        /// </summary>
        public XYData MassSpectrum { get; set; }

        /// <summary>
        /// Gets or sets the xic feature.
        /// </summary>
        public FeatureBlob XicFeature { get; set; }

        /// <summary>
        /// Gets or sets the isotopic fit score.
        /// </summary>
        public double IsotopicFitScore { get; set; }

        /// <summary>
        /// Gets or sets the analysis status.
        /// </summary>
        public AnalysisStatus AnalysisStatus { get; set; }

        /// <summary>
        /// Gets the intensity.
        /// </summary>
        public double Intensity
        {
            get { return this.IsotopicProfile.IntensityMostAbundant; }
        }
    }
}
