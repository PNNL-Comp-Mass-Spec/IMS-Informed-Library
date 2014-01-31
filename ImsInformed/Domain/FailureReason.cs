
namespace ImsInformed.Domain
{
	public enum FailureReason
	{
		None,
		Unknown,
		XicNotFound,
		IsotopicProfileNotFound,
		IsotopicProfileNotMatchTheoretical,
		IsotopicProfileNotEnoughPeaks,
		IsotopicFitScoreError,
		ElutionTimeError,
		DriftTimeError,
		MassError,
		PeakToLeft,
		ChargeStateCorrelation
	}
}
