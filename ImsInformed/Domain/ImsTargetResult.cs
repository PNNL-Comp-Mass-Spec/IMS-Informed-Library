using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend;
using DeconTools.Backend.Core;
using MultiDimensionalPeakFinding.PeakDetection;

namespace ImsInformed.Domain
{
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
		public double IsotopicFitScore { get; set; }
		public double PpmError { get; set; }

		public XYData MassSpectrum { get; set; }
		public FeatureBlob XicFeature { get; set; }

		public FailureReason FailureReason { get; set; }

		public double Intensity
		{
			get { return IsotopicProfile.IntensityMostAbundant; }
		}
	}
}
