// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CombinatorialIonTracker.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the CombinatorialIonTracker type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation.IonTrackers
{
    using System;
    using System.Collections.Generic;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Interfaces;

    /// <summary>
    /// Track ions by using combinations of observations and construct hypothesis.
    /// </summary>
    public class CombinatorialIonTracker : IIonTracker
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
        /// <exception cref="NotImplementedException">
        /// </exception>
        public AssociationHypothesis FindOptimumHypothesis(IEnumerable<ObservedPeak> observations)
        {
            throw new System.NotImplementedException();
        }
    }
}
