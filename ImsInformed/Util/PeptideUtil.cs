// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PeptideUtil.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the PeptideUtil type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Util
{
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Sequence;

    /// <summary>
    /// The peptide utilities.
    /// </summary>
    internal class PeptideUtil
    {
        /// <summary>
        /// The PPM constant.
        /// </summary>
        private const int PpmConst = 1000000;

        /// <summary>
        /// The _amino acid set.
        /// </summary>
        private static readonly AminoAcidSet AminoAcidSet;

        /// <summary>
        /// Initializes static members of the <see cref="PeptideUtil"/> class.
        /// </summary>
        static PeptideUtil()
        {
            AminoAcidSet = new AminoAcidSet();
        }

        /// <summary>
        /// The get composition of peptide.
        /// </summary>
        /// <param name="peptide">
        /// The peptide.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        public static Composition GetCompositionOfPeptide(string peptide)
        {
            Sequence peptideSequence = new Sequence(peptide, AminoAcidSet.GetStandardAminoAcidSet());
            var composition = peptideSequence.Composition;
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
            return (num2 - num1) / num2 * PpmConst;
        }
    }
}
