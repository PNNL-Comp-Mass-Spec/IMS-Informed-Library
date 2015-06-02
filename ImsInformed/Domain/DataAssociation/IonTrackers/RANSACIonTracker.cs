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
    using ImsInformed.Workflows.CrossSectionExtraction;

    /// <summary>
    /// The ransac ion tracker.
    /// </summary>
    internal class RANSACIonTracker : IIonTracker
    {
        public AssociationHypothesis FindOptimumHypothesis(
            IEnumerable<ObservedPeak> observations,
            double driftTubeLength,
            IImsTarget target,
            CrossSectionSearchParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
