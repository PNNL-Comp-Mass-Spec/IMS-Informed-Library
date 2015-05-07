// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RANSACIonTracker.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the RANSACIonTracker type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation.IonTrackers
{
    using System;
    using System.Collections.Generic;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Interfaces;

    /// <summary>
    /// The ransac ion tracker.
    /// </summary>
    public class RANSACIonTracker : IIonTracker
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
            throw new NotImplementedException();
        }
    }
}
