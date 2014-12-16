using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;

namespace ImsInformed.Util
{
	public class PeptideUtil
	{
		private const int PPM_CONST = 1000000;
		private static AminoAcidSet _aminoAcidSet;

		static PeptideUtil()
		{
			_aminoAcidSet = new AminoAcidSet();
		}

		public static Composition GetCompositionOfPeptide(string peptide)
		{
			var composition = _aminoAcidSet.GetComposition(peptide);
			composition += Composition.H2O;
			return composition;
		}

		/// <summary>
		/// Calculates the PPM error between two values.
		/// </summary>
		/// <param name="num1">Expected value.</param>
		/// <param name="num2">Observed value.</param>
		/// <returns>PPM error between expected and observed value.</returns>
		public static double PpmError(double num1, double num2)
		{
			// (X - Y) / X * 1,000,000
			return (num2 - num1) / num2 * PPM_CONST;
		}
	}
}
