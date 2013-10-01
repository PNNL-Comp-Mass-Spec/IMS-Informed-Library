using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ImsInformed.Domain;

namespace ImsInformed.IO
{
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

		public void AppendResultToCsv(ChargeStateCorrelationResult correlationResult)
		{
			ImsTarget target = correlationResult.ImsTarget;

			string modificationString = "";
			foreach (var modification in target.ModificationList)
			{
				modificationString += modification.Name + ":" + modification.AccessionNum + ";";
			}

			StringBuilder peptideInfo = new StringBuilder();
			peptideInfo.Append(target.Id + ",");
			peptideInfo.Append(target.Peptide + ",");
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
				resultInfo.Append(result.IsotopicFitScore + ",");
				resultInfo.Append(targetElutionTime + ",");
				resultInfo.Append(result.NormalizedElutionTime + ",");
				resultInfo.Append(elutionTimeError + ",");
				resultInfo.Append(targetDriftTime + ",");
				resultInfo.Append(result.DriftTime + ",");
				resultInfo.Append(driftTimeError + ",");
				resultInfo.Append(correlationAverage + ",");

				_textWriter.WriteLine(peptideInfo.ToString() + resultInfo.ToString());
			}
		}

		private static void AddCsvHeader(TextWriter textWriter)
		{
			const string header = "ID,Peptide,Mods,EmpiricalFormula,TargetMass,ChargeState,ObservedMz,ppmError,IsoFitScore,TargetElutionTime,ObservedElutionTime,ElutionTimeError,TargetDriftTime,ObservedDriftTime,DriftTimeError,ChargeCorrelation";
			textWriter.WriteLine(header);
		}

		public void Dispose()
		{
			_textWriter.Close();
		}
	}
}
