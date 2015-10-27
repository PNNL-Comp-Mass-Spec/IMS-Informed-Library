// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
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
