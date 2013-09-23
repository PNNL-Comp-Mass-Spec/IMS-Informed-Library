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
		}

		public string EmpiricalFormula { get; set; }
		public Composition Composition { get; set; }
		public string Peptide { get; set; }
		public double NormalizedElutionTime { get; set; }
		public IList<DriftTimeTarget> DriftTimeTargetList { get; set; }
		public IList<ImsTargetResult> ResultList { get; set; }
	}
}
