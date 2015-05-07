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

    using ImsInformed.Domain.DirectInjection;

    /// <summary>
    /// The ion tracking class that track ions across different voltage groups.
    /// </summary>
    public class AssociationHypothesis : IEquatable<AssociationHypothesis>
    {
        /// <summary>
        /// The total peaks.
        /// </summary>
        public IEnumerable<ObservedPeak> TotalPeaks
        {
            get
            {
                return this.totalPeaks;
            }
            
            private set
            {
                this.totalPeaks = value;
            }
        }

        /// <summary>
        /// The tracks.
        /// </summary>
        private HashSet<IsomerTrack> tracks;

        /// <summary>
        /// The on track features.
        /// </summary>
        private IDictionary<ObservedPeak, IsomerTrack> onTrackFeatures;

        private IEnumerable<ObservedPeak> totalPeaks;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssociationHypothesis"/> class.
        /// </summary>
        /// <param name="totalPeaks">
        /// The total Peaks.
        /// </param>
        public AssociationHypothesis(IEnumerable<ObservedPeak> totalPeaks)
        {
            this.totalPeaks = totalPeaks;
            this.tracks = new HashSet<IsomerTrack>();
            this.onTrackFeatures = new Dictionary<ObservedPeak, IsomerTrack>();
        }

        /// <summary>
        /// The probability of data given hypothesis.
        /// </summary>
        public double ProbabilityOfDataGivenHypothesis { get; private set; }

        /// <summary>
        /// The probability of hypothesis given data.
        /// </summary>
        public double ProbabilityOfHypothesisGivenData { get; private set; }

        /// <summary>
        /// The add isomer track.
        /// </summary>
        /// <param name="newTrack">
        /// The new track.
        /// </param>
        public void AddIsomerTrack(IsomerTrack newTrack)
        {
            this.tracks.Add(newTrack);
        }

        /// <summary>
        /// The compute posteriori probability for observation.
        /// </summary>
        /// <param name="observedPeak">
        /// The observed peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double ComputeAPosterioriProbabilityForObservation(ObservedPeak observedPeak)
        {
            return 1;
        }

        /// <summary>
        /// Return if input track violates the multually exclusive principle.
        /// </summary>
        /// <param name="track">
        /// The track.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsConflict(IsomerTrack track)
        {
            return true;
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
        /// Gets or sets the tracks.
        /// </summary>
        public IEnumerable<IsomerTrack> Tracks
        {
            get
            {
                return this.tracks;
            }
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
            throw new NotImplementedException();
            // Check if the peak is in the totalPeaks
            
            // Check if the peak is on the tracks 
        }
    }
}
