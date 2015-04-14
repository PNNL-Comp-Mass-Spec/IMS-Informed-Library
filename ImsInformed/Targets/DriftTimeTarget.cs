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
            : base(peptide.EmpiricalFormula, IonizationMethod.ProtonPlus, chargeState, libraryEntryName)
        {
            peptide.DriftTimeTargetList.Add(this);    

            this.NormalizedDriftTimeInMs = normalizedDriftTimeInMs;
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
            : base(empiricalFormula, ionizationMethod, adductMultiplier, libraryEntryName)
        {
            this.NormalizedDriftTimeInMs = normalizedDriftTimeInMs;
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
    }
}
