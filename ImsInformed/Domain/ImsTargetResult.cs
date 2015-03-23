// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImsTargetResult.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the ImsTargetResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    using DeconTools.Backend;
    using DeconTools.Backend.Core;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// The IMS target result.
    /// </summary>
    public class ImsTargetResult
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
            get { return IsotopicProfile.IntensityMostAbundant; }
        }
    }
}
