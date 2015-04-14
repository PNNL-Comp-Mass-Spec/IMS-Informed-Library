// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MolecularTarget.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the MolecularTarget type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Targets
{
    using System;
    using System.Globalization;

    using ImsInformed.Domain;
    using ImsInformed.Interfaces;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Composition;

    /// <summary>
    /// The molecular Target.
    /// </summary>
    [Serializable]
    public class MolecularTarget : IImsTarget
    {
        /// <summary>
        /// The chemical identifier.
        /// </summary>
        private string chemicalIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="MolecularTarget"/> class.
        /// </summary>
        /// <param name="targetMz">
        /// The Target MZ.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="chemicalIdentifier">
        /// The chemical Identifier.
        /// </param>
        public MolecularTarget(double targetMz, IonizationMethod ionization, string chemicalIdentifier)
        {
            this.MassWithAdduct = targetMz;
            this.TargetType = TargetType.Molecule;
            this.Adduct = new IonizationAdduct(ionization);
            this.chemicalIdentifier = chemicalIdentifier;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MolecularTarget"/> class. 
        /// Constructor for non peptides with composition
        /// </summary>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="chemicalIdentifier">
        /// The chemical Identifier.
        /// </param>
        public MolecularTarget(string empiricalFormula, IonizationAdduct ionization, string chemicalIdentifier)
        {
            this.chemicalIdentifier = chemicalIdentifier;
            this.Setup(empiricalFormula, ionization);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MolecularTarget"/> class.
        /// </summary>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="chemicalIdentifier">
        /// </param>
        /// <param name="adductMultiplier">
        /// The adductMultiplier.
        /// </param>
        public MolecularTarget(string empiricalFormula, IonizationMethod ionization, string chemicalIdentifier, int adductMultiplier = 1)
        {
            this.chemicalIdentifier = chemicalIdentifier;
            IonizationAdduct adduct = new IonizationAdduct(ionization, adductMultiplier);
            this.Setup(empiricalFormula, adduct);
        }

        /// <summary>
        /// Gets the mass.
        /// </summary>
        public double MonoisotopicMass { get; private set; }

        /// <summary>
        /// Gets the ionization type.
        /// </summary>
        public IonizationAdduct Adduct { get; private set; }

        /// <summary>
        /// Gets the Target type.
        /// </summary>
        public TargetType TargetType { get; private set; }

        /// <summary>
        /// Gets the empirical formula.
        /// </summary>
        public string EmpiricalFormula { get; private set; }

        /// <summary>
        /// Gets the composition with adduct.
        /// </summary>
        public Composition CompositionWithAdduct { get; private set; }

        /// <summary>
        /// Gets the composition without adduct.
        /// </summary>
        public Composition CompositionWithoutAdduct { get; private set; }

        /// <summary>
        /// Gets the chemical identifier.
        /// </summary>
        public string ChemicalIdentifier
        {
            get
            {
                return this.chemicalIdentifier;
            }
        }

        /// <summary>
        /// Gets or sets the mass with adduct.
        /// </summary>
        public double MassWithAdduct { get; set; }

        /// <summary>
        /// Gets the Target descriptor.
        /// </summary>
        public virtual string TargetDescriptor
        {
            get
            {
                return this.CompositionWithoutAdduct == null ? this.MassWithAdduct.ToString(CultureInfo.InvariantCulture) : string.Format(this.EmpiricalFormula + this.Adduct);
            }
        }

        /// <summary>
        /// Gets the charge state.
        /// </summary>
        public int ChargeState
        {
            get
            {
                return this.Adduct.ChargeState;
            }
        }

        /// <summary>
        /// The setup.
        /// </summary>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        private void Setup(string empiricalFormula, IonizationAdduct ionization)
        {
            this.EmpiricalFormula = empiricalFormula;

            // parse the small molecule empirical formula
            this.CompositionWithoutAdduct = MoleculeUtil.ReadEmpiricalFormula(empiricalFormula);
            
            // Compensate for mass changes due to ionization
            this.CompositionWithAdduct = ionization.ModifyComposition(this.CompositionWithoutAdduct);
            this.MonoisotopicMass = this.CompositionWithoutAdduct.Mass;
            this.MassWithAdduct = this.CompositionWithAdduct.Mass;
            this.TargetType = TargetType.Molecule;
            this.Adduct = ionization;
        }
    }
}
