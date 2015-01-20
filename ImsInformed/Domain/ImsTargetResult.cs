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
    /// The ims target result.
    /// </summary>
    public class ImsTargetResult
    {
        public int ChargeState { get; set; }
        public FeatureBlobStatistics FeatureBlobStatistics { get; set; }
        public int ScanLcRep { get; set; }
        public double NormalizedElutionTime { get; set; }
        public double DriftTime { get; set; }
        public double MonoisotopicMass { get; set; }
        public bool IsSaturated { get; set; }

        public IsotopicProfile IsotopicProfile { get; set; }
        public double IsotoicFitScore { get; set; }
        public double PpmError { get; set; }

        public XYData MassSpectrum { get; set; }
        public FeatureBlob XicFeature { get; set; }
        public double IsotopicFitScore { get; set; }
        public AnalysisStatus AnalysisStatus { get; set; }

        public double Intensity
        {
            get { return IsotopicProfile.IntensityMostAbundant; }
        }
    }
}
