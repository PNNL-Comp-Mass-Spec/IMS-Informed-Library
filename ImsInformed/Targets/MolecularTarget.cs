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
        private string sampleClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="MolecularTarget"/> class.
        /// </summary>
        /// <param name="targetMz">
        /// The Target MZ.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="sampleClass">
        /// The chemical Identifier.
        /// </param>
        public MolecularTarget(double targetMz, IonizationMethod ionization, string sampleClass)
        {
            this.MassWithAdduct = targetMz;
            this.TargetType = TargetType.Molecule;
            this.Adduct = new IonizationAdduct(ionization);
            this.sampleClass = sampleClass;
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
        /// <param name="sampleClass">
        /// The chemical Identifier.
        /// </param>
        public MolecularTarget(string empiricalFormula, IonizationAdduct ionization, string sampleClass)
        {
            this.sampleClass = sampleClass;
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
        /// <param name="sampleClass">
        /// </param>
        /// <param name="adductMultiplier">
        /// The adductMultiplier.
        /// </param>
        public MolecularTarget(string empiricalFormula, IonizationMethod ionization, string sampleClass, int adductMultiplier = 1)
        {
            this.sampleClass = sampleClass;
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
        public TargetType TargetType { get; protected set; }

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
        public string SampleClass
        {
            get
            {
                return this.sampleClass;
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
        /// Gets a value indicating whether has composition info.
        /// </summary>
        public bool HasCompositionInfo
        {
            get
            {
                return this.CompositionWithAdduct != null;
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

        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public virtual bool Equals(IImsTarget other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.TargetType != TargetType.Molecule) return false;
            return this.Equals(other as MolecularTarget);
        }

        public bool Equals(MolecularTarget other)
        {
            return MoleculeUtil.AreCompositionsEqual(this.CompositionWithAdduct, other.CompositionWithAdduct) && 
                this.ChargeState == other.ChargeState &&
                this.sampleClass == other.sampleClass;
        }

        public override bool Equals(object other) 
        {
            return this.Equals(other as IImsTarget);
        }

        /// <summary>
        /// The get hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode() 
        {
            int result = 29;
            result = result * 13 + this.CompositionWithAdduct.ToPlainString().GetHashCode();
            result = result * 13 + this.ChargeState;
            return result;
        }
    }
}
