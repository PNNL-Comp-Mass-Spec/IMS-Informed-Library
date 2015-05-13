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
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Sockets;
    using System.Windows.Controls.Primitives;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Interfaces;
    using ImsInformed.Stats;
    using ImsInformed.Workflows.CrossSectionExtraction;

    using MathNet.Numerics;

    using Combinatorics = ImsInformed.Stats.Combinatorics;

    /// <summary>
    /// Track ions by using combinations of observations and construct hypothesis.
    /// </summary>
    public class CombinatorialIonTracker : IIonTracker
    {
        private int trackPointsCounter;

        /// <summary>
        /// The find optimum hypothesis.
        /// </summary>
        /// <param name="observations">
        /// The observations.
        /// </param>
        /// <param name="driftTubeLength">
        /// The drift Tube Length.
        /// </param>
        /// <param name="massTarget">
        /// The mass Target.
        /// </param>
        /// <returns>
        /// The <see cref="AssociationHypothesis"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public AssociationHypothesis FindOptimumHypothesis(IEnumerable<ObservedPeak> observations, double driftTubeLength, IImsTarget massTarget, CrossSectionSearchParameters parameters)
        {
            // Create the data structure as the input to the combinatorial algorithm
            observations = observations.ToArray();
            IDictionary<VoltageGroup, IList<ObservedPeak>> basePeakMap = new Dictionary<VoltageGroup, IList<ObservedPeak>>();
            foreach (ObservedPeak observation in observations)
            {
                VoltageGroup voltageGroup = observation.VoltageGroup;
                if (basePeakMap.ContainsKey(voltageGroup))
                {
                    basePeakMap[voltageGroup].Add(observation);
                }
                else
                {
                    basePeakMap.Add(voltageGroup, new List<ObservedPeak> { observation });
                }
            }

            // Find all the possible combinotorial tracks
            IList<IsomerTrack> possibleTracks = this.FindAllReasonableTracks(basePeakMap, driftTubeLength, massTarget, parameters).ToList();

            IEnumerable<AssociationHypothesis> hypotheses = this.FindAllHypothesis(possibleTracks, observations);

            // Find the combination of tracks that produces the highest posterior probablity.
            double highestAPosteriorProbabiliy = 0;
            AssociationHypothesis optimalHypothesis = new AssociationHypothesis(observations);
            foreach (AssociationHypothesis hypothesis in hypotheses)
            {
                if (highestAPosteriorProbabiliy > hypothesis.ProbabilityOfHypothesisGivenData)
                {
                    highestAPosteriorProbabiliy = hypothesis.ProbabilityOfHypothesisGivenData;
                    optimalHypothesis = (AssociationHypothesis)hypothesis.Clone();
                }
            }

            return optimalHypothesis;
        }

        /// <summary>
        /// The find all hypothesis.
        /// </summary>
        /// <param name="validTracks">
        /// The valid tracks.
        /// </param>
        /// <param name="allPeaks"></param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        private IEnumerable<AssociationHypothesis> FindAllHypothesis(IEnumerable<IsomerTrack> validTracks, IEnumerable<ObservedPeak> allPeaks)
        {
            allPeaks = allPeaks.ToArray();
            IList<IsomerTrack> tracks = validTracks.ToList();
            int size = tracks.Count;

            int totalCombinations = (int)Math.Pow(2, size);
            
            AssociationHypothesis association = new AssociationHypothesis(allPeaks);

            bool hasConflicted = false;
            for (int i = 1; i < totalCombinations; i++)
            {
                long grey = Combinatorics.BinaryToGray(i);
                if (hasConflicted)
                {
                    association = new AssociationHypothesis(allPeaks);
                    IEnumerable<int> indexOfOnes = Combinatorics.GreyCodeToIndexOfOnes(grey);

                    foreach (var index in indexOfOnes)
                    {
                        if (!association.IsConflict(tracks[index]))
                        {
                            association.AddIsomerTrack(tracks[index]);
                        }
                        else
                        {
                            break;
                        }
                    }

                    yield return association;
                }
                else
                {
                    bool addOrNotRemove;
                    var index = Combinatorics.NextChangeOnGrey(i, out addOrNotRemove);

                    if (addOrNotRemove)
                    {   
                        IsomerTrack candiateTrack = tracks[index];
                        if (association.IsConflict(candiateTrack))
                        {
                            hasConflicted = true;
                        }
                        else
                        {
                            association.AddIsomerTrack(tracks[index]);
                            yield return association;
                        }
                    }
                    else
                    {
                        association.RemoveIsomerTrack(tracks[index]);
                        yield return association;
                    }
                }
            }
        }

        /// <summary>
        /// The find all track combinations.
        /// </summary>
        /// <param name="basePeakMap">
        /// The base peak map.
        /// </param>
        /// <param name="driftTubeLength">
        /// The drift tube length.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        private IEnumerable<IsomerTrack> FindAllReasonableTracks(IDictionary<VoltageGroup, IList<ObservedPeak>> basePeakMap, double driftTubeLength, IImsTarget target, CrossSectionSearchParameters parameters)
        {
            this.trackPointsCounter = 0;

            VoltageGroup[] sortedVoltageGroup = basePeakMap.Keys.OrderByDescending(vg => vg.MeanVoltageInVolts).ToArray();

            ObservedPeak[] selectedPeaks = new ObservedPeak[sortedVoltageGroup.Length];

            bool overflow = false;
            while (!overflow)
            {
                overflow = this.IncrementCombination(sortedVoltageGroup, ref selectedPeaks, basePeakMap, parameters.MinFitPoints);
                ObservedPeak[] realPeaks = selectedPeaks.Where(p => p != null).ToArray();
                IsomerTrack newTrack = new IsomerTrack(realPeaks, driftTubeLength);
                if (this.IsTrackPossible(newTrack, target, parameters))
                {
                    Trace.WriteLine(newTrack.TrackDescriptor);
                    yield return newTrack;
                }
            }            
        }

        /// <summary>
        /// The is track possible.
        /// </summary>
        /// <param name="track">
        /// The track.
        /// </param>
        /// <param name="target"></param>
        /// <param name="crossSectionSearchParameters"></param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool IsTrackPossible(IsomerTrack track, IImsTarget target, CrossSectionSearchParameters crossSectionSearchParameters)
        {
            if (track.RealPeakCount < crossSectionSearchParameters.MinFitPoints)
            {
                return false;
            }

            MobilityInfo trackMobilityInfo = track.GetMobilityInfoForTarget(target);
            if (!this.IsConsistentWithIonDynamics(trackMobilityInfo))
            {
                return false;
            }

            if (Filters.AnalysisFilter.IsLowR2(trackMobilityInfo.RSquared, crossSectionSearchParameters.MinR2))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// The is consistent with ion dynamics.
        /// </summary>
        /// <param name="info">
        /// The info.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool IsConsistentWithIonDynamics(MobilityInfo info)
        {
            if (info.Mobility < 0)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// The increment combination.
        /// </summary>
        /// <param name="groups">
        /// The groups.
        /// </param>
        /// <param name="selectedPeaks">
        /// The selected peaks.
        /// </param>
        /// <param name="basePeakMap">
        /// The base peak map.
        /// </param>
        /// <param name="minFitPoints">
        /// The min Fit Points.
        /// </param>
        /// <returns>
        /// If the increment overflows the "counter"<see cref="bool"/>.
        /// </returns>
        private bool IncrementCombination(VoltageGroup[] groups, ref ObservedPeak[] selectedPeaks, IDictionary<VoltageGroup, IList<ObservedPeak>> basePeakMap, int minFitPoints)
        {
            bool overflow = false;

            // So-called do while loop
            overflow = this.IncrementCombination(groups, ref selectedPeaks, 0, basePeakMap);

            while (!overflow && this.trackPointsCounter < minFitPoints)
            {
                overflow = this.IncrementCombination(groups, ref selectedPeaks, 0, basePeakMap);
            }

            return overflow;
        }

        /// <summary>
        /// Binary increment the selected peaks 
        /// </summary>
        /// <param name="groups">
        /// The groups.
        /// </param>
        /// <param name="selectedPeaks">
        /// The selected peaks.
        /// </param>
        /// <param name="onIndex">
        /// The on index.
        /// </param>
        /// <param name="basePeakMap">
        /// The base peak map.
        /// </param>
        /// <param name="minFitPoints">
        /// The min Fit Points.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        /// If incrementing overflows occurs/ all combinations were gone through.
        /// <exception cref="Exception">
        /// </exception>
        private bool IncrementCombination(VoltageGroup[] groups, ref ObservedPeak[] selectedPeaks, int onIndex, IDictionary<VoltageGroup, IList<ObservedPeak>> basePeakMap)
        {
            int peakIndex;

            if (groups.Length != selectedPeaks.Length)
            {
                throw new ArgumentException("Votlage group does not match selected peaks");
            }

            int length = selectedPeaks.Length;

            // Base case
            if (onIndex >= length)
            {
                return true;
            }
            
            ObservedPeak incrementPeak = selectedPeaks[onIndex];
            
            IList<ObservedPeak> candidates = basePeakMap[groups[onIndex]];

            // Empty peak
            if (incrementPeak == null)
            {
                peakIndex = -1;
            }
            else
            {
                peakIndex = candidates.IndexOf(incrementPeak);
                if (peakIndex < 0)
                {
                    throw new Exception("The combination to be incremented is not in the base peak map space.");
                }
            }
            
            if (peakIndex + 1 >= candidates.Count)
            {
                if (selectedPeaks[onIndex] != null)
                {
                    this.trackPointsCounter--;
                }
                selectedPeaks[onIndex] = null;
                

                return this.IncrementCombination(groups, ref selectedPeaks, onIndex + 1, basePeakMap);
            }
            else
            {
                if (peakIndex + 1 == 0)
                {
                    this.trackPointsCounter++;
                }

                selectedPeaks[onIndex] = candidates[peakIndex + 1];
                return false;
            }
        }
    }
}
