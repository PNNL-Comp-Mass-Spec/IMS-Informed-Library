// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIonTracker.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The IonTracker interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Interfaces
{
    using System.Collections;
    using System.Collections.Generic;

    using ImsInformed.Domain.DataAssociation;
    using ImsInformed.Domain.DirectInjection;

    /// <summary>
    /// The IonTracker interface.
    /// </summary>
    public interface IIonTracker
    {
        /// <summary>
        /// The find optimum hypothesis.
        /// </summary>
        /// <param name="observations">
        /// The observations.
        /// </param>
        /// <returns>
        /// The <see cref="AssociationHypothesis"/>.
        /// </returns>
        AssociationHypothesis FindOptimumHypothesis(IEnumerable<ObservedPeak> observations);
    }
}
