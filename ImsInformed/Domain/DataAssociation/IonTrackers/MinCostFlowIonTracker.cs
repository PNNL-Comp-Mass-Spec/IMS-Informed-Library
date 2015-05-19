// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MinCostFlowIonTracker.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the MinCostFlowIonTracker type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation.IonTrackers
{
    using System;
    using System.Collections.Generic;

    using ImsInformed.Domain.DataAssociation.IonSignatureMatching;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Interfaces;
    using ImsInformed.Workflows.CrossSectionExtraction;

    using QuickGraph;

    /// <summary>
    /// The min cost flow ion tracker.
    /// </summary>
    public class MinCostFlowIonTracker : IIonTracker
    {
        public AssociationHypothesis FindOptimumHypothesis(
            IEnumerable<ObservedPeak> observations,
            double driftTubeLength,
            IImsTarget target,
            CrossSectionSearchParameters parameters)
        {
            throw new NotImplementedException();
        }

        public static IsomerTrack ToTrack(IEnumerable<IonTransition> edges, double driftTubeLength)
        {
            IsomerTrack track = new IsomerTrack(driftTubeLength);
            foreach (IonTransition edge in edges)
            {
                track.AddIonTransition(edge);
                ObservedPeak target = edge.Target;
                if (target.ObservationType != ObservationType.Virtual)
                {
                    track.AddObservation(target);
                }
            }

            return track;
        }

        /// <summary>
        /// The to tracks.
        /// </summary>
        /// <param name="edges">
        /// The edges.
        /// </param>
        /// <param name="driftTubeLength">
        /// The drift tube length.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public static IEnumerable<IsomerTrack> ToTracks(IEnumerable<IEnumerable<IonTransition>> edges, double driftTubeLength)
        {
            foreach (IEnumerable<IonTransition> rawTrack in edges)
            {
                IsomerTrack track = ToTrack(rawTrack, driftTubeLength);

                yield return track;
            }
        }
    }
}
