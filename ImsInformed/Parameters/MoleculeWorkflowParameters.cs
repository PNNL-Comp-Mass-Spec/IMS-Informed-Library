using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Parameters
{
	public class MoleculeWorkflowParameters : InformedParameters
	{
		public double FeatureFilterLevel { get; set; }
        public double ConfidenceThreshold { get; set; }
        public int FeatureScoreThreshold { get; set; }

        public MoleculeWorkflowParameters()
        {
            // default values
            this.IsotopicFitScoreMax = 0.15;
            this.MassToleranceInPpm = 10;
            this.NumPointForSmoothing = 9;
            this.ConfidenceThreshold = 0.5;
            this.FeatureFilterLevel = 0.25;
            this.FeatureScoreThreshold = 2;
        }
	}
}
