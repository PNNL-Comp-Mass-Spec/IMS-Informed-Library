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
