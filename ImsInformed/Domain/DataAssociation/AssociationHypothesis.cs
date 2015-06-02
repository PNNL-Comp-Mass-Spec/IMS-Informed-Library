// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssociationHypothesis.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The ion tracking.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Scoring;

    /// <summary>
    /// The ion tracking class that track ions across different voltage groups.
    /// </summary>
    internal class AssociationHypothesis : IEquatable<AssociationHypothesis>, ICloneable
    {
        /// <summary>
        /// The all observations.
        /// </summary>
        private readonly IEnumerable<ObservedPeak> allObservations;

        /// <summary>
        /// The tracks.
        /// </summary>
        private IList<IsomerTrack> tracks;

        /// <summary>
        /// The on track features.
        /// </summary>
        private IDictionary<ObservedPeak, IsomerTrack> onTrackObservations;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssociationHypothesis"/> class.
        /// </summary>
        /// <param name="allObservations">
        /// The total Peaks.
        /// </param>
        public AssociationHypothesis(IEnumerable<ObservedPeak> allObservations)
        {
            this.allObservations = allObservations;
            this.tracks = new List<IsomerTrack>();
            this.onTrackObservations = new Dictionary<ObservedPeak, IsomerTrack>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssociationHypothesis"/> class.
        /// </summary>
        /// <param name="associationHypothesis">
        /// The association hypothesis.
        /// </param>
        private AssociationHypothesis(AssociationHypothesis associationHypothesis)
        {
            this.allObservations = associationHypothesis.AllObservations; // reference copy the immutable observation collection
            this.tracks = associationHypothesis.tracks.ToList(); // shallow copy list
            this.onTrackObservations = new Dictionary<ObservedPeak, IsomerTrack>(associationHypothesis.onTrackObservations);
        }

        /// <summary>
        /// Gets the probability of data given hypothesis.
        /// </summary>
        public double ProbabilityOfDataGivenHypothesis
        {
            get
            {
                double p = 1;
                foreach (var observation in this.allObservations)
                {
                    p *= this.ComputeAPosterioriProbabilityForObservation(observation);
                }

                return p;
            }
        }

        /// <summary>
        /// Gets the probability of hypothesis given observations.
        /// </summary>
        public double ProbabilityOfHypothesisGivenData
        {
            get
            {
                double p = 1;
                foreach (IsomerTrack track in this.tracks)
                {
                    p *= track.TrackProbability;
                }

                return this.ProbabilityOfDataGivenHypothesis * p;
            }
        }

        /// <summary>
        /// Gets the tracks.
        /// </summary>
        public IEnumerable<IsomerTrack> Tracks
        {
            get
            {
                return this.tracks;
            }
        }

        /// <summary>
        /// Gets the on track observations.
        /// </summary>
        public IEnumerable<ObservedPeak> OnTrackObservations
        {
            get
            {
                return this.onTrackObservations.Keys;
            }
        }

        /// <summary>
        /// Gets all the observations in the hypothesis.
        /// </summary>
        public IEnumerable<ObservedPeak> AllObservations
        {
            get
            {
                return this.allObservations;
            }
        }

        /// <summary>
        /// The add isomer track.
        /// </summary>
        /// <param name="newTrack">
        /// The new track.
        /// </param>
        public void AddIsomerTrack(IsomerTrack newTrack)
        {
            // Add the track
            this.tracks.Add(newTrack);
            this.RegisterObservationsOfTrack(newTrack);
        }

        /// <summary>
        /// Remove the most Recent isomer track.
        /// </summary>
        public void RemoveIsomerTrack(IsomerTrack track)
        {
            this.tracks.Remove(track);
            this.onTrackObservations = new Dictionary<ObservedPeak, IsomerTrack>();
            foreach (var trackLeft in this.tracks)
            {
                this.RegisterObservationsOfTrack(trackLeft);
            }
        }

        /// <summary>
        /// Remove the most Recent isomer track.
        /// </summary>
        public void RemoveAllIsomerTracks()
        {
            this.tracks = new List<IsomerTrack>();
            this.onTrackObservations = new Dictionary<ObservedPeak, IsomerTrack>();
        }

        /// <summary>
        /// The compute posteriori probability for observation Pr(xi | T)
        /// </summary>
        /// <param name="observedPeak">
        /// The observed peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double ComputeAPosterioriProbabilityForObservation(ObservedPeak observedPeak)
        {
            if (this.IsOnTrack(observedPeak))
            {
                IsomerTrack track = this.GetTrack(observedPeak);
                double driftTimeErrorInSeconds = track.GetDriftTimeErrorInSecondsForObsevation(observedPeak);
                double pOn = ScoreUtil.MapToZeroOneExponential(driftTimeErrorInSeconds, DataAssociationParameters.DriftTimeDifferenceInMs09 / 1000, 0.9, true);
                return pOn;
            }
            else
            {
                // TODO I honestly don't know how this should be evalued other than evaluating as a constant.
                double pOff = DataAssociationParameters.PxTOutlier;
                return pOff;
            }
        }

        /// <summary>
        /// The is conflict.
        /// </summary>
        /// <param name="track">
        /// The track.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsConflict(IsomerTrack track)
        {
            IList<IsomerTrack> conflictedTracks;
            return this.IsConflict(track, out conflictedTracks);
        }

        /// <summary>
        /// Return if input track violates the mutually exclusive principle.
        /// </summary>
        /// <param name="track">
        /// The track.
        /// </param>
        /// <param name="conflictedTracks">
        /// The conflicted Tracks.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsConflict(IsomerTrack track, out IList<IsomerTrack> conflictedTracks)
        {
            bool result = false;
            conflictedTracks = new List<IsomerTrack>();
            foreach (ObservedPeak peak in track.ObservedPeaks)
            {
                if (this.onTrackObservations.Keys.Contains(peak))
                {
                    conflictedTracks.Add(this.onTrackObservations[peak]);
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Get all ion tracks
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public bool Equals(AssociationHypothesis other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The is on track.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <returns>
        /// if the given observation is on tracks specified by the association hypothesis <see cref="bool"/>.
        /// </returns>
        public bool IsOnTrack(ObservedPeak peak)
        {
            return this.onTrackObservations.ContainsKey(peak);
        }

        /// <summary>
        /// The get track.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <returns>
        /// The <see cref="IsomerTrack"/>.
        /// </returns>
        public IsomerTrack GetTrack(ObservedPeak peak)
        {
            if (!this.IsOnTrack(peak))
            {
                return null;
            }
            else
            {
                return this.onTrackObservations[peak];
            }
        }

        /// <summary>
        /// The clone.
        /// </summary>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Clone()
        {
            return new AssociationHypothesis(this);
        }

        /// <summary>
        /// The register observations of track.
        /// </summary>
        /// <param name="track">
        /// The track.
        /// </param>
        private void RegisterObservationsOfTrack(IsomerTrack track)
        {
            foreach (var peak in track.ObservedPeaks)
            {
                if (!this.onTrackObservations.Keys.Contains(peak))
                {
                    this.onTrackObservations.Add(peak, track);
                }
            }
        }
    }
}
