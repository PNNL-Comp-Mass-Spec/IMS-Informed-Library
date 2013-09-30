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
		public ImsTarget(string peptide, double normalizedElutionTime)
		{
			this.Peptide = peptide;
			this.NormalizedElutionTime = normalizedElutionTime;
			this.Composition = PeptideUtil.GetCompositionOfPeptide(peptide);
			this.EmpiricalFormula = this.Composition.ToPlainString();
			this.DriftTimeTargetList = new List<DriftTimeTarget>();
			this.ResultList = new List<ImsTargetResult>();
			this.ModificationList = new List<Modification>();
		}

		public ImsTarget(string peptide, double normalizedElutionTime, IList<Modification> modificationList)
		{
			Composition composition = PeptideUtil.GetCompositionOfPeptide(peptide);
			foreach (var modification in modificationList)
			{
				composition += modification.Composition;
			}

			this.Peptide = peptide;
			this.NormalizedElutionTime = normalizedElutionTime;
			this.Composition = composition;
			this.EmpiricalFormula = this.Composition.ToPlainString();
			this.DriftTimeTargetList = new List<DriftTimeTarget>();
			this.ResultList = new List<ImsTargetResult>();
			this.ModificationList = modificationList;
		}

		public string EmpiricalFormula { get; private set; }
		public Composition Composition { get; private set; }
		public string Peptide { get; private set; }
		public double NormalizedElutionTime { get; private set; }
		public IList<DriftTimeTarget> DriftTimeTargetList { get; set; }
		public IList<ImsTargetResult> ResultList { get; set; }
		public IList<Modification> ModificationList { get; private set; }
	}
}
