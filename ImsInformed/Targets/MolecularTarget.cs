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
    using System.Collections.Generic;
    using System.Globalization;

    using ImsInformed.Domain;
    using ImsInformed.Interfaces;
    using ImsInformed.Util;
    using ImsInformed.Workflows.LcImsPeptideExtraction;

    using InformedProteomics.Backend.Data.Composition;

    public class MolecularTarget : IImsTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImsTarget"/> class.
        /// </summary>
        /// <param name="id">
        /// The ID.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="targetMz">
        /// The target MZ.
        /// </param>
        public MolecularTarget(int id, IonizationMethod ionization, double targetMz)
        {
            this.ID = id;
            this.TargetMz = targetMz;
            this.TargetType = TargetType.Molecule;
            this.IonizationType = ionization;
        }

        /// <summary>
        /// Constructor for non peptides with composition
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        public MolecularTarget(int id, IonizationMethod ionization, string empiricalFormula)
        {
            this.ID = id;
            this.EmpiricalFormula = empiricalFormula;

            // parse the small molecule empirical formula
            this.Composition = MoleculeUtil.ReadEmpiricalFormula(empiricalFormula);
            this.Mass = this.Composition.Mass;
            this.TargetType = TargetType.Molecule;
            this.IonizationType = ionization;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public int ID { get; private set; }

                /// <summary>
        /// Gets the mass.
        /// </summary>
        public double Mass { get; private set; }

        /// <summary>
        /// Gets the ionization type.
        /// </summary>
        public IonizationMethod IonizationType { get; private set; }

        /// <summary>
        /// Gets the target type.
        /// </summary>
        public TargetType TargetType { get; private set; }

        /// <summary>
        /// Gets the empirical formula.
        /// </summary>
        public string EmpiricalFormula { get; private set; }

        public Composition Composition { get; private set; }

        /// <summary>
        /// Gets or sets the target MZ.
        /// </summary>
        public double TargetMz { get; set; }

        public string TargetDescriptor
        {
            get
            {
                return this.Composition == null ? this.TargetMz.ToString(CultureInfo.InvariantCulture) : this.EmpiricalFormula;;
            }
        }
    }
}
