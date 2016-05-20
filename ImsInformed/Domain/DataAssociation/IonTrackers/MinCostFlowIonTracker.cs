// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
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
namespace ImsInformed.Domain.DataAssociation.IonTrackers
{
    using System;
    using System.Collections.Generic;

    using ImsInformed.Domain.DataAssociation.IonSignatureMatching;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Statistics;
    using ImsInformed.Targets;
    using ImsInformed.Workflows.CrossSectionExtraction;

    using QuickGraph;

    /// <summary>
    /// The min cost flow ion tracker.
    /// </summary>
    internal class MinCostFlowIonTracker : IIonTracker
    {
        public AssociationHypothesis FindOptimumHypothesis(
            IEnumerable<ObservedPeak> observations,
            double driftTubeLength,
            IImsTarget target,
            CrossSectionSearchParameters parameters)
        {
            throw new NotImplementedException();
        }

        public static IsomerTrack ToTrack(IEnumerable<IonTransition> edges, CrossSectionSearchParameters parameters, int totalNumberOfVoltageGroups, FitLine fitline)
        {
            IsomerTrack track = new IsomerTrack(parameters.DriftTubeLengthInCm, totalNumberOfVoltageGroups, fitline, parameters.UseAverageTemperature);
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
        /// <param name="parameters"></param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public static IEnumerable<IsomerTrack> ToTracks(IEnumerable<IEnumerable<IonTransition>> edges, CrossSectionSearchParameters parameters, int numberOfVoltageGroups, FitlineEnum fitlineType)
        {
            foreach (IEnumerable<IonTransition> rawTrack in edges)
            {
                FitLine newFitline = (fitlineType == FitlineEnum.OrdinaryLeastSquares) ? (FitLine)new LeastSquaresFitLine() : new IRLSFitline(10000);
                IsomerTrack track = ToTrack(rawTrack, parameters, numberOfVoltageGroups, newFitline);

                yield return track;
            }
        }

        public AssociationHypothesis FindOptimumHypothesis(
            IEnumerable<ObservedPeak> observations,
            double driftTubeLength,
            IImsTarget target,
            CrossSectionSearchParameters parameters,
            int numberOfVoltageGroups)
        {
            throw new NotImplementedException();
        }
    }
}
