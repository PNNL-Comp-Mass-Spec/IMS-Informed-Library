using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Parameters
{
	public class InformedParameters
	{
		public double NetTolerance { get; set; }
		public double DriftTimeTolerance { get; set; }
		public double IsotopicFitScoreMax { get; set; }
		public double MassToleranceInPpm { get; set; }
		public int ChargeStateMax { get; set; }
		public int NumPointForSmoothing { get; set; }
	}
}
