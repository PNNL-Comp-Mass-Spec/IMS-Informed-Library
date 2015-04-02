// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChargeStateCorrelationResult.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the ChargeStateCorrelationResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows.LcImsPeptideExtraction
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using ImsInformed.Targets;

    using PNNLOmics.Data;

    public class ChargeStateCorrelationResult
    {
        public PeptideTarget ImsTarget { get; private set; }
        public LcImsTargetResult ReferenceLcImsTargetResult { get; private set; }
        public Dictionary<LcImsTargetResult, double> CorrelationMap { get; private set; }

        public List<LcImsTargetResult> CorrelatedResults { get; private set; }
        public double CorrelationSum { get; private set; }

        public ChargeStateCorrelationResult(PeptideTarget imsTarget, LcImsTargetResult referenceLcImsTargetResult)
        {
            this.ImsTarget = imsTarget;
            this.ReferenceLcImsTargetResult = referenceLcImsTargetResult;
            this.CorrelationMap = new Dictionary<LcImsTargetResult, double>();
        }

        public double GetBestCorrelation(out List<LcImsTargetResult> correlatedResults)
        {
            double correlationSum = 0;
            correlatedResults = new List<LcImsTargetResult> { this.ReferenceLcImsTargetResult };

            var chargeStateGrouping = this.CorrelationMap.GroupBy(x => x.Key.ChargeState);
            foreach (var group in chargeStateGrouping)
            {
                int chargeState = group.Key;

                // No need to consider correlation to the same charge state
                if (chargeState == this.ReferenceLcImsTargetResult.ChargeState) continue;

                // Grab the result that has the best correlation to the reference result
                var highestMatch = group.OrderByDescending(x => x.Value).First();
                LcImsTargetResult highestCorrelatedFeature = highestMatch.Key;
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
            List<LcImsTargetResult> correlatedResults;
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
                updateResultQuery.Append(this.ImsTarget.ID);
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
