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
	}
}
