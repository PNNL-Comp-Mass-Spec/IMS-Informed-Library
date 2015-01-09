using System.Collections.Generic;

namespace ImsInformed.Domain
{
	public class FailurePrecedence
	{
		public static Dictionary<AnalysisStatus, double> FailurePredenceMap = new Dictionary<AnalysisStatus, double>
		{
			{ AnalysisStatus.POS, 0 },
			{ AnalysisStatus.XicNotFound, 1 },
			{ AnalysisStatus.IsotopicProfileNotFound, 2 },
			{ AnalysisStatus.IsotopicProfileNotMatchTheoretical, 2.1 },
			{ AnalysisStatus.IsotopicProfileNotEnoughPeaks, 2.2 },
			{ AnalysisStatus.IsotopicFitScoreError, 3 },
			{ AnalysisStatus.ElutionTimeError, 4 },
			{ AnalysisStatus.DriftTimeError, 5 },
			{ AnalysisStatus.MassError, 6 },
			{ AnalysisStatus.PeakToLeft, 7 },
			{ AnalysisStatus.ChargeStateCorrelation, 8 },
			{ AnalysisStatus.ERR, 9 }
		};
	}
}
