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
    using System.Globalization;

    using ImsInformed.Domain;
    using ImsInformed.Interfaces;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Composition;

    /// <summary>
    /// The molecular target.
    /// </summary>
    public class MolecularTarget : IImsTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MolecularTarget"/> class.
        /// </summary>
        /// <param name="targetMz">
        /// The target MZ.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        public MolecularTarget(double targetMz, IonizationMethod ionization)
        {
            this.MassWithAdduct = targetMz;
            this.TargetType = TargetType.Molecule;
            this.Adduct = new IonizationAdduct(ionization);
        }

        /// <summary>
        /// Constructor for non peptides with composition
        /// </summary>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        public MolecularTarget(string empiricalFormula, IonizationAdduct ionization)
        {
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
        /// <param name="adductMultiplier">
        /// The adductMultiplier.
        /// </param>
        public MolecularTarget(string empiricalFormula, IonizationMethod ionization, int adductMultiplier = 1)
        {
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
        /// Gets the target type.
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
        /// Gets or sets the mass with adduct.
        /// </summary>
        public double MassWithAdduct { get; set; }

        /// <summary>
        /// Gets the target descriptor.
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
