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
    using System.Collections.Generic;

    using ImsInformed.Domain.DataAssociation;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Workflows.CrossSectionExtraction;

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
        /// <param name="driftTubeLength">
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="AssociationHypothesis"/>.
        /// </returns>
        AssociationHypothesis FindOptimumHypothesis(IEnumerable<ObservedPeak> observations, double driftTubeLength, IImsTarget target, CrossSectionSearchParameters parameters);
    }
}
