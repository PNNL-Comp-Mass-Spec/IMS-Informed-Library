using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
	public class ChargeStateCorrelationResult
	{
		public ImsTargetResult ReferenceImsTargetResult { get; private set; }
		public Dictionary<ImsTargetResult, double> CorrelationMap { get; set; }

		public ChargeStateCorrelationResult(ImsTargetResult referenceImsTargetResult)
		{
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
				
				// Add to list and add to correlation total
				correlatedResults.Add(highestCorrelatedFeature);
				correlationSum += correlationValue;
			}

			return correlationSum;
		}
	}
}
