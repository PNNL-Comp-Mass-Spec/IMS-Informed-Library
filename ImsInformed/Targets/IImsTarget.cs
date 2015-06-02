// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IImsTarget.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the IImsTarget type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Targets
{
    using System;

    using InformedProteomics.Backend.Data.Composition;

    /// <summary>
    /// The IMS Target interface.
    /// </summary>
    public interface IImsTarget : IEquatable<IImsTarget>
    {
        /// <summary>
        /// Gets the mass.
        /// </summary>
        double MonoisotopicMass { get; }

        /// <summary>
        /// Gets the typicalIonization type.
        /// </summary>
        IonizationAdduct Adduct { get; }

        /// <summary>
        /// Gets the Target type.
        /// </summary>
        TargetType TargetType { get; }

        /// <summary>
        /// Gets the empirical formula.
        /// </summary>
        string EmpiricalFormula { get; }

        /// <summary>
        /// Gets the composition.
        /// </summary>
        Composition CompositionWithoutAdduct { get; }

        /// <summary>
        /// Gets the composition.
        /// </summary>
        Composition CompositionWithAdduct { get; }

        /// <summary>
        /// Gets the target descriptor, human-readable information of what the target is about in terms of drift time, MZ, and so on.
        /// </summary>
        string TargetDescriptor { get; }

        /// <summary>
        /// Gets the chemical identifier, googleable, adduct independent information of what the chemical is.
        /// </summary>
        string ChemicalIdentifier { get; }

        /// <summary>
        /// Gets the Target m/z.
        /// </summary>
        double MassWithAdduct { get; }

        /// <summary>
        /// Gets the Target m/z.
        /// </summary>
        int ChargeState { get; }

        /// <summary>
        /// Gets a value indicating whether has composition info.
        /// </summary>
        bool HasCompositionInfo { get; }
    }
}
