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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ImsInformed.Domain;

namespace ImsInformed.IO
{
    using ImsInformed.Targets;
    using ImsInformed.Workflows.LcImsPeptideExtraction;

    public class ImsTargetResultExporter : IDisposable
    {
        public StreamWriter TextWriter;

        public ImsTargetResultExporter(string outputLocation)
        {
            FileInfo outputFileInfo = new FileInfo(outputLocation);
            if (File.Exists(outputFileInfo.FullName)) File.Delete(outputFileInfo.FullName);
            this.TextWriter = new StreamWriter(outputFileInfo.FullName) { AutoFlush = true };
            AddCsvHeader(this.TextWriter);
        }

        public void AppendResultsOfTargetToCsv(PeptideTarget target)
        {
            string modificationString = "";
            foreach (var modification in target.ModificationList)
            {
                modificationString += modification.Name + ":" + modification.AccessionNum + ";";
            }

            StringBuilder peptideInfo = new StringBuilder();
            peptideInfo.Append(target.ID + ",");
            peptideInfo.Append(target.PeptideSequence + ",");
            peptideInfo.Append(modificationString + ","); // TODO: Mods
            peptideInfo.Append(target.EmpiricalFormula + ",");
            peptideInfo.Append(target.MonoisotopicMass + ",");

            double targetElutionTime = target.NormalizedElutionTime;

            foreach (LcImsTargetResult result in target.ResultList)
            {
                int chargeState = result.ChargeState;
                IEnumerable<DriftTimeTarget> possibleDriftTimeTargets = target.DriftTimeTargetList.Where(x => x.ChargeState == chargeState).OrderBy(x => Math.Abs(x.NormalizedDriftTimeInMs - result.DriftTime));

                double targetDriftTime = 0;
                double driftTimeError = 0;

                if (possibleDriftTimeTargets.Any())
                {
                    DriftTimeTarget driftTimeTarget = possibleDriftTimeTargets.First();
                    targetDriftTime = driftTimeTarget.NormalizedDriftTimeInMs;
                    driftTimeError = result.DriftTime - targetDriftTime;
                }

                double observedMz = result.IsotopicProfile != null ? result.IsotopicProfile.MonoPeakMZ : 0;
                double abundance = result.IsotopicProfile != null ? result.IsotopicProfile.GetAbundance() : 0;
                double elutionTimeError = result.NormalizedElutionTime - targetElutionTime;
                double correlationAverage = 0;

                StringBuilder resultInfo = new StringBuilder();
                resultInfo.Append(result.ChargeState + ",");
                resultInfo.Append(observedMz + ",");
                resultInfo.Append(result.PpmError + ",");
                resultInfo.Append(result.ScanLcRep + ",");
                resultInfo.Append(result.IsotopicFitScore + ",");
                resultInfo.Append(abundance + ",");
                resultInfo.Append(targetElutionTime + ",");
                resultInfo.Append(result.NormalizedElutionTime + ",");
                resultInfo.Append(elutionTimeError + ",");
                resultInfo.Append(targetDriftTime + ",");
                resultInfo.Append(result.DriftTime + ",");
                resultInfo.Append(driftTimeError + ",");
                resultInfo.Append(correlationAverage + ",");
                resultInfo.Append(result.AnalysisStatus.ToString());

                this.TextWriter.WriteLine(peptideInfo.ToString() + resultInfo.ToString());
            }
        }

        /// <summary>
        /// The append correlation result to csv.
        /// </summary>
        /// <param name="correlationResult">
        /// The correlation result.
        /// </param>
        public void AppendCorrelationResultToCsv(ChargeStateCorrelationResult correlationResult)
        {
            PeptideTarget target = correlationResult.ImsTarget;

            string modificationString = "";
            foreach (var modification in target.ModificationList)
            {
                modificationString += modification.Name + ":" + modification.AccessionNum + ";";
            }

            StringBuilder peptideInfo = new StringBuilder();
            peptideInfo.Append(target.ID + ",");
            peptideInfo.Append(target.PeptideSequence + ",");
            peptideInfo.Append(modificationString + ","); // TODO: Mods
            peptideInfo.Append(target.EmpiricalFormula + ",");
            peptideInfo.Append(target.MonoisotopicMass + ",");

            double targetElutionTime = target.NormalizedElutionTime;

            foreach (var result in correlationResult.CorrelatedResults)
            {
                int chargeState = result.ChargeState;
                IEnumerable<DriftTimeTarget> possibleDriftTimeTargets = target.DriftTimeTargetList.Where(x => x.ChargeState == chargeState).OrderBy(x => Math.Abs(x.NormalizedDriftTimeInMs - result.DriftTime));

                double targetDriftTime = 0;
                double driftTimeError = 0;

                if(possibleDriftTimeTargets.Any())
                {
                    DriftTimeTarget driftTimeTarget = possibleDriftTimeTargets.First();
                    targetDriftTime = driftTimeTarget.NormalizedDriftTimeInMs;
                    driftTimeError = result.DriftTime - targetDriftTime;
                }

                double elutionTimeError = result.NormalizedElutionTime - targetElutionTime;
                double correlationAverage = correlationResult.CorrelationSum / (correlationResult.CorrelatedResults.Count - 1);

                StringBuilder resultInfo = new StringBuilder();
                resultInfo.Append(result.ChargeState + ",");
                resultInfo.Append(result.IsotopicProfile.MonoPeakMZ + ",");
                resultInfo.Append(result.PpmError + ",");
                resultInfo.Append(result.ScanLcRep + ",");
                resultInfo.Append(result.IsotopicFitScore + ",");
                resultInfo.Append(result.IsotopicProfile.GetAbundance() + ",");
                resultInfo.Append(targetElutionTime + ",");
                resultInfo.Append(result.NormalizedElutionTime + ",");
                resultInfo.Append(elutionTimeError + ",");
                resultInfo.Append(targetDriftTime + ",");
                resultInfo.Append(result.DriftTime + ",");
                resultInfo.Append(driftTimeError + ",");
                resultInfo.Append(correlationAverage + ",");
                resultInfo.Append(result.AnalysisStatus.ToString());

                this.TextWriter.WriteLine(peptideInfo.ToString() + resultInfo.ToString());
            }
        }

        private static void AddCsvHeader(TextWriter textWriter)
        {
            const string header = "ID,PeptideSequence,Mods,EmpiricalFormula,TargetMass,ChargeState,ObservedMz,ppmError,ScanLcRep,IsoFitScore,Abundance,TargetElutionTime,ObservedElutionTime,ElutionTimeError,TargetDriftTime,ObservedDriftTime,DriftTimeError,ChargeCorrelation,AnalysisStatus";
            textWriter.WriteLine(header);
        }

        public void Dispose()
        {
            this.TextWriter.Close();
        }
    }
}
