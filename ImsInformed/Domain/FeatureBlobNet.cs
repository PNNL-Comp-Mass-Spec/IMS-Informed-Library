using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiDimensionalPeakFinding.PeakDetection;

namespace ImsInformed.Domain
{
	public class FeatureBlobNet
	{
		public FeatureBlob FeatureBlob { get; private set; }
		public double NormalizedElutionTime { get; private set; }

		public FeatureBlobNet(FeatureBlob featureBlob, double normalizedElutionTime)
		{
			this.FeatureBlob = featureBlob;
			this.NormalizedElutionTime = normalizedElutionTime;
		}
	}
}
