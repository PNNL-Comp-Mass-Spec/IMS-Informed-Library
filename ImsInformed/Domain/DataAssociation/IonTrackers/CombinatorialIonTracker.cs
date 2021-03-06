﻿// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.

using System.IO;
using MathNet.Numerics.Statistics;

namespace ImsInformed.Domain.DataAssociation.IonTrackers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain.DataAssociation.IonSignatureMatching;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Filters;
    using ImsInformed.Statistics;
    using ImsInformed.Targets;
    using ImsInformed.Workflows.CrossSectionExtraction;

    using QuickGraph.Algorithms;

    /// <summary>
    /// Track ions by using combinations of observations and construct hypothesis.
    /// </summary>
    internal class CombinatorialIonTracker : IIonTracker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CombinatorialIonTracker"/> class.
        /// </summary>
        /// <param name="maxTracks">
        /// The max tracks.
        /// </param>
        public CombinatorialIonTracker(int maxTracks)
        {
            this.maxTracks = maxTracks;
        }

        private int maxTracks;

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
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <returns>
        /// The <see cref="AssociationHypothesis"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public AssociationHypothesis FindOptimumHypothesis(IEnumerable<ObservedPeak> observations, double driftTubeLength, IImsTarget massTarget, CrossSectionSearchParameters parameters, int numberOfVoltageGroups)
        {
            observations = observations.ToList();

            ObservationTransitionGraph<IonTransition> transitionGraph = new ObservationTransitionGraph<IonTransition>(observations, (a, b) => new IonTransition(a, b));
            
            // Visualize the graph.
            //transitionGraph.PlotGraph();
            
            // Find all the possible combinotorial tracks
            // IList<IsomerTrack> candidateTracks = this.FindAllReasonableTracks(transitionGraph, driftTubeLength, massTarget, parameters).ToList();
            
            // Find the top N tracks using K shorestest path algorithm
            IEnumerable<IEnumerable<IonTransition>> kShorestPaths = transitionGraph.PeakGraph.RankedShortestPathHoffmanPavley(t => 0 - Math.Log(t.TransitionProbability), transitionGraph.SourceVertex, transitionGraph.SinkVertex, this.maxTracks);

            IEnumerable<IsomerTrack> candidateTracks = MinCostFlowIonTracker.ToTracks(kShorestPaths, parameters, numberOfVoltageGroups, parameters.RegressionSelection);

            // filter paths
            TrackFilter filter = new TrackFilter();
            Predicate<IsomerTrack> trackPredicate = track => filter.IsTrackPossible(track, massTarget, parameters);
            List<IsomerTrack> filteredTracks = candidateTracks.ToList().FindAll(trackPredicate);
            var sortedFilteredTracks = filteredTracks.OrderByDescending(t => t.FitLine.RSquared);

            // Select the top N tracks to proceed to next step.
            var hypotheses = this.FindAllHypothesis(sortedFilteredTracks, observations).ToArray();

            // Find the combination of tracks that produces the highest posterior probablity.
          IOrderedEnumerable<AssociationHypothesis> sortedAssociationHypotheses = hypotheses.OrderByDescending(h => h.ProbabilityOfHypothesisGivenData);

            return sortedAssociationHypotheses.FirstOrDefault();
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
            List<IsomerTrack> tracks = validTracks.ToList();

            const int AlgorithmLimit = 20;
            int size = tracks.Count;

            if (size >= AlgorithmLimit)
            {
                tracks = tracks.GetRange(0, AlgorithmLimit);
                size = tracks.Count;
            }

            int totalCombinations = (int)Math.Pow(2, size) - 1;
            
            AssociationHypothesis association = new AssociationHypothesis(allPeaks);

            bool hasConflicted = false;
            for (int i = 0; i < totalCombinations - 1; i++)
            {
                long grey = Combinatorics.BinaryToGray(i + 1);
                if (hasConflicted)
                {
                    association = new AssociationHypothesis(allPeaks);
                    hasConflicted = false;
                    int[] indexOfOnes = Combinatorics.GreyCodeToIndexOfOnes(grey).ToArray();

                    foreach (var index in indexOfOnes)
                    {
                        hasConflicted = association.IsConflict(tracks[index]);
                        if (!hasConflicted)
                        {
                            association.AddIsomerTrack(tracks[index]);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!hasConflicted)
                    {
                        yield return (AssociationHypothesis)association.Clone();
                    }
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
                            yield return (AssociationHypothesis)association.Clone();
                        }
                    }
                    else
                    {
                        association.RemoveIsomerTrack(tracks[index]);
                        yield return (AssociationHypothesis)association.Clone();
                    }
                }
            }
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
        private bool IncrementCombination(VoltageGroup[] groups, ref ObservedPeak[] selectedPeaks, ObservationTransitionGraph<IonTransition> graph, int minFitPoints)
        {
            bool overflow = false;

            // So-called do while loop
            overflow = this.IncrementCombination(groups, ref selectedPeaks, 0, graph);

            while (!overflow && this.trackPointsCounter < minFitPoints)
            {
                overflow = this.IncrementCombination(groups, ref selectedPeaks, 0, graph);
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
        /// <param name="graph">
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
        private bool IncrementCombination(VoltageGroup[] groups, ref ObservedPeak[] selectedPeaks, int onIndex, ObservationTransitionGraph<IonTransition> graph)
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
            VoltageGroup group = groups[onIndex];

            IList<ObservedPeak> candidates = graph.FindPeaksInVoltageGroup(group).ToList();

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
                

                return this.IncrementCombination(groups, ref selectedPeaks, onIndex + 1, graph);
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
