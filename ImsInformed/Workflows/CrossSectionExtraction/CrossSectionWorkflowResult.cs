// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrossSectionInformedWorkflowResult.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the CrossSectionInformedWorkflowResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DataAssociation;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Filters;
    using ImsInformed.Interfaces;
    using ImsInformed.Scoring;
    using ImsInformed.Util;

    /// <summary>
    /// The molecule informed workflow result.
    /// </summary>
    [Serializable]
    public class CrossSectionWorkflowResult
    {
        /// <summary>
        /// The dataset analyzed.
        /// </summary>
        public readonly string DatasetName;

        /// <summary>
        /// The target searched against.
        /// </summary>
        public readonly IImsTarget Target;

        /// <summary>
        /// The analysis status.
        /// </summary>
        public readonly AnalysisStatus AnalysisStatus;

        /// <summary>
        /// The best feature score.
        /// </summary>
        public readonly FeatureStatistics AverageObservedPeakStatistics;

        /// <summary>
        /// The best feature score.
        /// </summary>
        public readonly double AverageVoltageGroupStability;

        /// <summary>
        /// The r squared.
        /// </summary>
        public readonly AssociationHypothesisInfo AssociationHypothesisInfo;

        /// <summary>
        /// The isomer results.
        /// </summary>
        private readonly IList<IdentifiedIsomerInfo> isomerResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionWorkflowResult"/> class. 
        /// Multiple isomer result constructor
        /// </summary>
        /// <param name="datasetName">
        /// The dataset name.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="analysisStatus">
        /// The analysis status.
        /// </param>
        /// <param name="associationHypothesisInfo">
        /// The analysis scores holder.
        /// </param>
        /// <param name="isomerResults">
        /// The isomer results.
        /// </param>
        /// <param name="averageObservedPeakStatistics"></param>
        /// <param name="averageVoltageGroupStability"></param>
        public CrossSectionWorkflowResult(
            string datasetName, 
            IImsTarget target, 
            AnalysisStatus analysisStatus, 
            AssociationHypothesisInfo associationHypothesisInfo, 
            IList<IdentifiedIsomerInfo> isomerResults, 
            FeatureStatistics averageObservedPeakStatistics, 
            double averageVoltageGroupStability)
        {
            this.DatasetName = datasetName;
            this.Target = target;
            this.AnalysisStatus = analysisStatus;
            this.AssociationHypothesisInfo = associationHypothesisInfo;
            this.isomerResults = isomerResults;
            this.AverageObservedPeakStatistics = averageObservedPeakStatistics;
            this.AverageVoltageGroupStability = averageVoltageGroupStability;
        }

        /// Initializes a new instance of the <see cref="CrossSectionWorkflowResult"/> class. 
        /// Constructor for no isomer result. 
        public CrossSectionWorkflowResult(
            string datasetName,
            IImsTarget target,
            AnalysisStatus analysisStatus,
            AssociationHypothesisInfo associationHypothesisInfo, 
            FeatureStatistics averageObservedPeakStatistics, 
            double averageVoltageGroupStability)
            : this(
                datasetName,
                target,
                analysisStatus,
                associationHypothesisInfo,
                new List<IdentifiedIsomerInfo>(), averageObservedPeakStatistics, averageVoltageGroupStability)
        {
        }

        /// <summary>
        /// The isomer results.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public IEnumerable<IdentifiedIsomerInfo> IdentifiedIsomers
        {
            get 
            {
                return this.isomerResults;
            }
        }

        /// <summary>
        /// The create error result.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="datasetName">
        /// The dataset name.
        /// </param>
        /// <returns>
        /// The <see cref="CrossSectionWorkflowResult"/>.
        /// </returns>
        public static CrossSectionWorkflowResult CreateErrorResult(IImsTarget target, string datasetName)
        {
            // No hypothesis can be made.
            return new CrossSectionWorkflowResult(
                datasetName, 
                target, 
                AnalysisStatus.UknownError,
                null, 
                null,
                null, 
                0);
        }

        /// <summary>
        /// The create negative result.
        /// </summary>
        /// <param name="rejectedPeaks">
        /// The rejected peaks.
        /// </param>
        /// <param name="rejectedVoltageGroups">
        /// The rejected voltage groups.
        /// </param>
        /// <param name="datasetName">
        /// The dataset name.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="CrossSectionWorkflowResult"/>.
        /// </returns>
        public static CrossSectionWorkflowResult CreateNegativeResult(IEnumerable<ObservedPeak> rejectedPeaks, IEnumerable<VoltageGroup> rejectedVoltageGroups, string datasetName, IImsTarget target)
        {
            // No valid feature peaks were identified. No hypothesis.
            double voltageGroupScore = VoltageGroupScore.ComputeAverageVoltageGroupStabilityScore(rejectedVoltageGroups);
            
            // quantize the VG score from VGs in the removal list.
            IEnumerable<FeatureStatistics> featureStats = rejectedPeaks.Select(x => x.Statistics);
            FeatureStatistics averageFeatureStatistics = FeatureScoreUtilities.AverageFeatureStatistics(featureStats);
            CrossSectionWorkflowResult informedResult = new CrossSectionWorkflowResult(
                datasetName, 
                target, 
                AnalysisStatus.Negative, 
                null,
                averageFeatureStatistics,
                voltageGroupScore);

            return informedResult;
        }

        public static CrossSectionWorkflowResult CreateResultFromAssociationHypothesis(CrossSectionSearchParameters parameters, string datasetName, AssociationHypothesis optimalHypothesis, IImsTarget target, IEnumerable<VoltageGroup> allVoltageGroups, IEnumerable<ObservedPeak> allPeaks, double viperCompatibleMass = 0)
        {
            // Initialize the result struct.
            AssociationHypothesisInfo associationHypothesisInfo = new AssociationHypothesisInfo(optimalHypothesis.ProbabilityOfDataGivenHypothesis, optimalHypothesis.ProbabilityOfHypothesisGivenData);

            double averageVoltageGroupScore = VoltageGroupScore.ComputeAverageVoltageGroupStabilityScore(allVoltageGroups);
            IEnumerable<FeatureStatistics> allFeatureStatistics = allPeaks.Select(x => x.Statistics);
            FeatureStatistics averageObservedPeakStatistics = FeatureScoreUtilities.AverageFeatureStatistics(allFeatureStatistics);

            IEnumerable<IsomerTrack> tracks = optimalHypothesis.Tracks;
            IList<IdentifiedIsomerInfo> isomersInfo = tracks.Select(x => x.ExportIdentifiedIsomerInfo(viperCompatibleMass, parameters.MinFitPoints, parameters.MinR2)).ToList();
            
            AnalysisStatus finalStatus = TrackToHypothesisConclusionLogic(isomersInfo.Select(info => info.AnalysisStatus));

            CrossSectionWorkflowResult informedResult = new CrossSectionWorkflowResult(
            datasetName, 
            target,
            finalStatus, 
            associationHypothesisInfo,
            isomersInfo,
            averageObservedPeakStatistics,
            averageVoltageGroupScore);
            
            return informedResult;
        }
        
        private static AnalysisStatus TrackToHypothesisConclusionLogic(IEnumerable<AnalysisStatus> trackConclusions)
        {
            AnalysisStatus result = AnalysisStatus.Rejected;
            foreach (var status in trackConclusions)
            {
                if (status == AnalysisStatus.Positive)
                {
                    return AnalysisStatus.Positive;
                }
                else if (result == AnalysisStatus.Rejected)
                {
                    result = AnalysisStatus.Rejected;
                }
                else if (result == AnalysisStatus.NotSufficientPoints)
                {
                    if (result != AnalysisStatus.Rejected)
                    {
                        result = AnalysisStatus.NotSufficientPoints;
                    }
                }
            }

            return result;
        }
    }
}
