using System.Collections.Generic;

namespace ImsInformed.Domain
{
	public class FailurePrecedence
	{
		public static Dictionary<FailureReason, double> FailurePredenceMap = new Dictionary<FailureReason, double>
		{
			{ FailureReason.None, 0 },
			{ FailureReason.XicNotFound, 1 },
			{ FailureReason.IsotopicProfileNotFound, 2 },
			{ FailureReason.IsotopicProfileNotMatchTheoretical, 2.1 },
			{ FailureReason.IsotopicProfileNotEnoughPeaks, 2.2 },
			{ FailureReason.IsotopicFitScoreError, 3 },
			{ FailureReason.ElutionTimeError, 4 },
			{ FailureReason.DriftTimeError, 5 },
			{ FailureReason.MassError, 6 },
			{ FailureReason.PeakToLeft, 7 },
			{ FailureReason.ChargeStateCorrelation, 8 },
			{ FailureReason.Unknown, 9 }
		};
	}
}
