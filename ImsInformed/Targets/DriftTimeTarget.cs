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
namespace ImsInformed.Targets
{
    using System;
    using System.Collections.Generic;

    using ImsInformed.Domain;

    /// <summary>
    /// The drift time Target.
    /// </summary>
    [Serializable]
    public class DriftTimeTarget : MolecularTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DriftTimeTarget"/> class with H+ as default
        /// ionization, which is a default for peptides.
        /// </summary>
        /// <param name="libraryEntryName">
        /// The library Entry Name.
        /// </param>
        /// <param name="normalizedDriftTimeInMs">
        /// The drift time.
        /// </param>
        /// <param name="peptide">
        /// The peptide.
        /// </param>
        /// <param name="chargeState">
        /// The charge State.
        /// </param>
        public DriftTimeTarget(string libraryEntryName, double normalizedDriftTimeInMs, PeptideTarget peptide, int chargeState = 1)
            : base(peptide.EmpiricalFormula, IonizationMethod.Protonated, libraryEntryName, chargeState)
        {
            peptide.DriftTimeTargetList.Add(this);    

            this.NormalizedDriftTimeInMs = normalizedDriftTimeInMs;
            this.TargetType = TargetType.MoleculeWithKnownDriftTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriftTimeTarget"/> class for molercules
        /// </summary>
        /// <param name="libraryEntryName">
        /// The library Entry Name.
        /// </param>
        /// <param name="normalizedDriftTimeInMs">
        /// The drift time.
        /// </param>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="ionizationMethod">
        /// The ionization method.
        /// </param>
        /// <param name="adductMultiplier">
        /// The adduct Multiplier.
        /// </param>
        public DriftTimeTarget(string libraryEntryName, double normalizedDriftTimeInMs, string empiricalFormula, IonizationMethod ionizationMethod, int adductMultiplier = 1)
            : base(empiricalFormula, ionizationMethod, libraryEntryName, adductMultiplier)
        {
            this.NormalizedDriftTimeInMs = normalizedDriftTimeInMs;
            this.TargetType = TargetType.MoleculeWithKnownDriftTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriftTimeTarget"/> class.
        /// </summary>
        /// <param name="libraryEntryName">
        /// The library Entry Name.
        /// </param>
        /// <param name="normalizedDriftTimeInMs">
        /// The drift time.
        /// </param>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="adduct">
        /// The adduct.
        /// </param>
        public DriftTimeTarget(string libraryEntryName, double normalizedDriftTimeInMs, string empiricalFormula, IonizationAdduct adduct)
            : base(empiricalFormula, adduct, libraryEntryName)
        {
            this.NormalizedDriftTimeInMs = normalizedDriftTimeInMs;
            this.TargetType = TargetType.MoleculeWithKnownDriftTime; 
        }

        /// <summary>
        /// Gets the Target descriptor.
        /// </summary>
        public override string TargetDescriptor
        {
            get
            {
                return string.Format("[{0}{1}, {2:F2} ms(normalized)]", this.EmpiricalFormula, this.Adduct, this.NormalizedDriftTimeInMs);
            }
        }

        /// <summary>
        /// Gets the drift time.
        /// </summary>
        public double NormalizedDriftTimeInMs { get; private set; }

        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(IImsTarget other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.TargetType != TargetType.MoleculeWithKnownDriftTime) return false;
            return this.Equals(other as DriftTimeTarget);
        }

        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool Equals(DriftTimeTarget other)
        {
            return base.Equals(other) && Math.Abs(this.NormalizedDriftTimeInMs - other.NormalizedDriftTimeInMs) < 0.00001;
        }
    }
}
