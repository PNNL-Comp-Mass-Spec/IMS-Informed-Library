using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImsInformed.Util;
using InformedProteomics.Backend.Data.Sequence;

namespace ImsInformed.Domain
{
	public class ImsTarget
	{
		public ImsTarget(int id, string peptide, double normalizedElutionTime)
		{
			this.Id = id;
			this.Peptide = peptide;
			this.Composition = PeptideUtil.GetCompositionOfPeptide(peptide);
			this.Mass = this.Composition.GetMass();
			this.NormalizedElutionTime = normalizedElutionTime;
			this.EmpiricalFormula = this.Composition.ToPlainString();
			this.DriftTimeTargetList = new List<DriftTimeTarget>();
			this.ResultList = new List<ImsTargetResult>();
			this.ModificationList = new List<Modification>();
		}

		public ImsTarget(int id, string peptide, double normalizedElutionTime, IList<Modification> modificationList)
		{
			Composition composition = PeptideUtil.GetCompositionOfPeptide(peptide);
			foreach (var modification in modificationList)
			{
				composition += modification.Composition;
			}

			this.Id = id;
			this.Peptide = peptide;
			this.Mass = composition.GetMass();
			this.NormalizedElutionTime = normalizedElutionTime;
			this.Composition = composition;
			this.EmpiricalFormula = this.Composition.ToPlainString();
			this.DriftTimeTargetList = new List<DriftTimeTarget>();
			this.ResultList = new List<ImsTargetResult>();
			this.ModificationList = modificationList;
		}

		public int Id { get; private set; }
		public string EmpiricalFormula { get; private set; }
		public Composition Composition { get; private set; }
		public string Peptide { get; private set; }
		public double Mass { get; private set; }
		public double NormalizedElutionTime { get; private set; }
		public IList<DriftTimeTarget> DriftTimeTargetList { get; set; }
		public IList<ImsTargetResult> ResultList { get; set; }
		public IList<Modification> ModificationList { get; private set; }

		public void RemoveResults()
		{
			this.ResultList = new List<ImsTargetResult>();
		}

		public string CreateSqlMassTagQueries()
		{
			StringBuilder massTagQuery = new StringBuilder();
			massTagQuery.Append("INSERT INTO T_MASS_TAG (Mass_Tag_Id, Peptide, Mod_Description, Empirical_Formula, Monoisotopic_Mass, NET) VALUES(");
			massTagQuery.Append(this.Id);
			massTagQuery.Append(",");
			massTagQuery.Append("'" + this.Peptide + "'");
			massTagQuery.Append(",");
			massTagQuery.Append("'MOD_HERE'");
			massTagQuery.Append(",");
			massTagQuery.Append("'" + this.EmpiricalFormula + "'");
			massTagQuery.Append(",");
			massTagQuery.Append(this.Mass);
			massTagQuery.Append(",");
			massTagQuery.Append(this.NormalizedElutionTime);
			massTagQuery.Append(");");

			return massTagQuery.ToString();
		}

		public string CreateSqlResultQueries(int datasetId)
		{
			StringBuilder allQueries = new StringBuilder();

			//StringBuilder massTagQuery = new StringBuilder();
			//massTagQuery.Append("INSERT INTO T_MASS_TAG (Mass_Tag_Id, Peptide, Mod_Description, Empirical_Formula, Monoisotopic_Mass, NET) VALUES(");
			//massTagQuery.Append(this.Id);
			//massTagQuery.Append(",");
			//massTagQuery.Append("'" + this.Peptide + "'");
			//massTagQuery.Append(",");
			//massTagQuery.Append("'MOD_HERE'");
			//massTagQuery.Append(",");
			//massTagQuery.Append("'" + this.EmpiricalFormula + "'");
			//massTagQuery.Append(",");
			//massTagQuery.Append(this.Mass);
			//massTagQuery.Append(",");
			//massTagQuery.Append(this.NormalizedElutionTime);
			//massTagQuery.Append(");");

			//allQueries.Append(massTagQuery.ToString());

			//foreach (var driftTimeTarget in this.DriftTimeTargetList)
			//{
			//    StringBuilder conformerQuery = new StringBuilder();
			//    conformerQuery.Append("INSERT INTO T_MASS_TAG_Conformer (Mass_Tag_Id, Charge_State, Drift_Time) VALUES(");
			//    conformerQuery.Append(this.Id);
			//    conformerQuery.Append(",");
			//    conformerQuery.Append(driftTimeTarget.ChargeState);
			//    conformerQuery.Append(",");
			//    conformerQuery.Append(driftTimeTarget.DriftTime);
			//    conformerQuery.Append(");");

			//    allQueries.Append("\n");
			//    allQueries.Append(conformerQuery.ToString());
			//}

			foreach (var imsTargetResult in this.ResultList)
			{
				double observedMz = imsTargetResult.ObservedMz;
				double abundance = imsTargetResult.IsotopicProfile != null ? imsTargetResult.Intensity: 0;
				int chargeState = imsTargetResult.ChargeState;
				double driftTime = imsTargetResult.DriftTime;

				IEnumerable<DriftTimeTarget> possibleDriftTimeTargets = this.DriftTimeTargetList.Where(x => x.ChargeState == chargeState).OrderBy(x => Math.Abs(x.DriftTime - driftTime));

				double targetDriftTime = 0;
				double driftTimeError = 0;

				if (possibleDriftTimeTargets.Any())
				{
					DriftTimeTarget driftTimeTarget = possibleDriftTimeTargets.First();
					targetDriftTime = driftTimeTarget.DriftTime;
					driftTimeError = driftTime - targetDriftTime;
				}

				double elutionTimeError = imsTargetResult.NormalizedElutionTime - this.NormalizedElutionTime;

				StringBuilder resultQuery = new StringBuilder();
				resultQuery.Append("INSERT INTO T_Result (Mass_Tag_Id, Dataset_Id, Charge_State, Observed_Mz, Ppm_Error, Scan_Lc, Net, Net_Error, Drift_Time, Drift_Time_Error, Isotopic_Fit_Score, Abundance, Charge_Correlation, Failure_Reason) VALUES(");
				resultQuery.Append(this.Id);
				resultQuery.Append(",");
				resultQuery.Append(datasetId);
				resultQuery.Append(",");
				resultQuery.Append(chargeState);
				resultQuery.Append(",");
				resultQuery.Append(observedMz);
				resultQuery.Append(",");
				resultQuery.Append(imsTargetResult.PpmError);
				resultQuery.Append(",");
				resultQuery.Append(imsTargetResult.ScanLcRep);
				resultQuery.Append(",");
				resultQuery.Append(imsTargetResult.NormalizedElutionTime);
				resultQuery.Append(",");
				resultQuery.Append(elutionTimeError);
				resultQuery.Append(",");
				resultQuery.Append(driftTime);
				resultQuery.Append(",");
				resultQuery.Append(driftTimeError);
				resultQuery.Append(",");
				resultQuery.Append(imsTargetResult.IsotopicFitScore);
				resultQuery.Append(",");
				resultQuery.Append(abundance);
				resultQuery.Append(",");
				resultQuery.Append("0");
				resultQuery.Append(",");
				resultQuery.Append("'" + imsTargetResult.FailureReason + "'");
				resultQuery.Append(");");

				allQueries.Append("\n");
				allQueries.Append(resultQuery.ToString());
			}

			return allQueries.ToString();
		}
	}
}
