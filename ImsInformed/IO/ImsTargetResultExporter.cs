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
        public StreamWriter _textWriter;

        public ImsTargetResultExporter(string outputLocation)
        {
            FileInfo outputFileInfo = new FileInfo(outputLocation);
            if (File.Exists(outputFileInfo.FullName)) File.Delete(outputFileInfo.FullName);
            _textWriter = new StreamWriter(outputFileInfo.FullName) { AutoFlush = true };
            AddCsvHeader(_textWriter);
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
            peptideInfo.Append(target.Mass + ",");

            double targetElutionTime = target.NormalizedElutionTime;

            foreach (LcImsTargetResult result in target.ResultList)
            {
                int chargeState = result.ChargeState;
                IEnumerable<DriftTimeTarget> possibleDriftTimeTargets = target.DriftTimeTargetList.Where(x => x.ChargeState == chargeState).OrderBy(x => Math.Abs(x.DriftTime - result.DriftTime));

                double targetDriftTime = 0;
                double driftTimeError = 0;

                if (possibleDriftTimeTargets.Any())
                {
                    DriftTimeTarget driftTimeTarget = possibleDriftTimeTargets.First();
                    targetDriftTime = driftTimeTarget.DriftTime;
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

                _textWriter.WriteLine(peptideInfo.ToString() + resultInfo.ToString());
            }
        }

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
            peptideInfo.Append(target.Mass + ",");

            double targetElutionTime = target.NormalizedElutionTime;

            foreach (var result in correlationResult.CorrelatedResults)
            {
                int chargeState = result.ChargeState;
                IEnumerable<DriftTimeTarget> possibleDriftTimeTargets = target.DriftTimeTargetList.Where(x => x.ChargeState == chargeState).OrderBy(x => Math.Abs(x.DriftTime - result.DriftTime));

                double targetDriftTime = 0;
                double driftTimeError = 0;

                if(possibleDriftTimeTargets.Any())
                {
                    DriftTimeTarget driftTimeTarget = possibleDriftTimeTargets.First();
                    targetDriftTime = driftTimeTarget.DriftTime;
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

                _textWriter.WriteLine(peptideInfo.ToString() + resultInfo.ToString());
            }
        }

        private static void AddCsvHeader(TextWriter textWriter)
        {
            const string header = "ID,PeptideSequence,Mods,EmpiricalFormula,TargetMass,ChargeState,ObservedMz,ppmError,ScanLcRep,IsoFitScore,Abundance,TargetElutionTime,ObservedElutionTime,ElutionTimeError,TargetDriftTime,ObservedDriftTime,DriftTimeError,ChargeCorrelation,AnalysisStatus";
            textWriter.WriteLine(header);
        }

        public void Dispose()
        {
            _textWriter.Close();
        }
    }
}
