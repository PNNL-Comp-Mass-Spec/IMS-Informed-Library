using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
	public class ChargeStateCorrelationResult
	{
		public ImsTarget ImsTarget { get; private set; }
		public ImsTargetResult ReferenceImsTargetResult { get; private set; }
		public Dictionary<ImsTargetResult, double> CorrelationMap { get; private set; }

		public List<ImsTargetResult> CorrelatedResults { get; private set; }
		public double CorrelationSum { get; private set; }

		public ChargeStateCorrelationResult(ImsTarget imsTarget, ImsTargetResult referenceImsTargetResult)
		{
			this.ImsTarget = imsTarget;
			this.ReferenceImsTargetResult = referenceImsTargetResult;
			this.CorrelationMap = new Dictionary<ImsTargetResult, double>();
		}

		public double GetBestCorrelation(out List<ImsTargetResult> correlatedResults)
		{
			double correlationSum = 0;
			correlatedResults = new List<ImsTargetResult> { this.ReferenceImsTargetResult };

			var chargeStateGrouping = this.CorrelationMap.GroupBy(x => x.Key.ChargeState);
			foreach (var group in chargeStateGrouping)
			{
				int chargeState = group.Key;

				// No need to consider correlation to the same charge state
				if (chargeState == this.ReferenceImsTargetResult.ChargeState) continue;

				// Grab the result that has the best correlation to the reference result
				var highestMatch = group.OrderByDescending(x => x.Value).First();
				ImsTargetResult highestCorrelatedFeature = highestMatch.Key;
				double correlationValue = highestMatch.Value;

				// Do not consider any low correlations
				if (correlationValue < 0.8) continue;
				
				// Add to list and add to correlation total
				correlatedResults.Add(highestCorrelatedFeature);
				correlationSum += correlationValue;
			}

			this.CorrelatedResults = correlatedResults;
			this.CorrelationSum = correlationSum;

			return correlationSum;
		}

		public string CreateSqlUpdateQueries()
		{
			List<ImsTargetResult> correlatedResults;
			double correlationSum = this.GetBestCorrelation(out correlatedResults);
			double correlationAverage = correlationSum / (this.CorrelatedResults.Count - 1);

			if (double.IsNaN(correlationSum) || double.IsNaN(correlationAverage)) return "";

			StringBuilder allQueries = new StringBuilder();

			foreach (var result in correlatedResults)
			{
				StringBuilder updateResultQuery = new StringBuilder();
				updateResultQuery.Append("UPDATE T_Result SET Charge_Correlation = ");
				updateResultQuery.Append(correlationAverage);
				updateResultQuery.Append(" WHERE Mass_Tag_Id = ");
				updateResultQuery.Append(this.ImsTarget.Id);
				updateResultQuery.Append(" AND Charge_State = ");
				updateResultQuery.Append(result.ChargeState);
				updateResultQuery.Append(" AND Scan_Lc = ");
				updateResultQuery.Append(result.ScanLcRep);
				updateResultQuery.Append(" AND Drift_Time = ");
				updateResultQuery.Append(result.DriftTime);
				updateResultQuery.Append(";");

				allQueries.Append(updateResultQuery.ToString());
				allQueries.Append("\n");
			}

			return allQueries.ToString();
		}
	}
}
