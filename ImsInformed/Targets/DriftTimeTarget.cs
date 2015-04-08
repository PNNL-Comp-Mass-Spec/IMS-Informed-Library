// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DriftTimeTarget.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the DriftTimeTarget type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Targets
{
    using ImsInformed.Domain;

    /// <summary>
    /// The drift time target.
    /// </summary>
    public class DriftTimeTarget : MolecularTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DriftTimeTarget"/> class with H+ as default
        /// ionization, which is a default for peptides.
        /// </summary>
        /// <param name="driftTime">
        /// The drift time.
        /// </param>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="adductMultiplier">
        /// The adduct multiplier.
        /// </param>
        public DriftTimeTarget(double driftTime, PeptideTarget peptide, int chargeState = 1)
            : base(peptide.EmpiricalFormula, IonizationMethod.ProtonPlus, chargeState)
        {
            this.DriftTime = driftTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriftTimeTarget"/> class for molercules
        /// </summary>
        /// <param name="driftTime">
        /// The drift time.
        /// </param>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="ionizationMethod">
        /// The ionization method.
        /// </param>
        public DriftTimeTarget(double driftTime, string empiricalFormula, IonizationMethod ionizationMethod, int adductMultiplier = 1)
            : base(empiricalFormula, ionizationMethod, adductMultiplier)
        {
            this.DriftTime = driftTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriftTimeTarget"/> class.
        /// </summary>
        /// <param name="driftTime">
        /// The drift time.
        /// </param>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="adduct">
        /// The adduct.
        /// </param>
        public DriftTimeTarget(double driftTime, string empiricalFormula, IonizationAdduct adduct)
            : base(empiricalFormula, adduct)
        {
            this.DriftTime = driftTime;
        }

        /// <summary>
        /// Gets the drift time.
        /// </summary>
        public double DriftTime { get; private set; }
    }
}
