// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IImsTarget.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the IImsTarget type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Interfaces
{
    using System.Globalization;

    using ImsInformed.Domain;

    using InformedProteomics.Backend.Data.Composition;

    /// <summary>
    /// The IMS Target interface.
    /// </summary>
    public interface IImsTarget
    {
        /// <summary>
        /// Gets the id.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// Gets the mass.
        /// </summary>
        double Mass { get; }

        /// <summary>
        /// Gets the ionization type.
        /// </summary>
        IonizationMethod IonizationType { get; }

        /// <summary>
        /// Gets the target type.
        /// </summary>
        TargetType TargetType { get; }

        /// <summary>
        /// Gets the empirical formula.
        /// </summary>
        string EmpiricalFormula { get; }

        /// <summary>
        /// Gets the composition.
        /// </summary>
        Composition Composition { get; }

        /// <summary>
        /// Gets the target descriptor.
        /// </summary>
        string TargetDescriptor { get; }

        /// <summary>
        /// Gets or sets the target mz.
        /// </summary>
        double TargetMz { get; set; }
    }
}
