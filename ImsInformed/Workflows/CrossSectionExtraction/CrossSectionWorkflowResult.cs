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
namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using ImsInformed.Domain.DataAssociation;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Scoring;
    using ImsInformed.Targets;
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
        /// The dataset analyzed.
        /// </summary>
        public readonly string DatasetPath;

        /// <summary>
        /// The dataset analyzed.
        /// </summary>
        public readonly string DateTime;

         /// <summary>
        /// The dataset analyzed.
        /// </summary>
        public readonly string AnalysisDirectory;

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
        public readonly PeakScores AverageObservedPeakStatistics;

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

        /// Initializes a new instance of the <see cref="CrossSectionWorkflowResult"/> class. 
        /// Constructor for no isomer result. 
        public CrossSectionWorkflowResult(
            string datasetPath,
            IImsTarget target,
            AnalysisStatus analysisStatus,
            AssociationHypothesisInfo associationHypothesisInfo, 
            PeakScores averageObservedPeakStatistics, 
            double averageVoltageGroupStability, 
            string analysisDirectory, string dateTime)
            : this(
            datasetPath,
            target,
            analysisStatus,
            associationHypothesisInfo,
            new List<IdentifiedIsomerInfo>(), 
            averageObservedPeakStatistics, 
            averageVoltageGroupStability, 
            analysisDirectory, 
            dateTime)
        {
        }

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
        /// <param name="datasetPath"></param>
        /// <param name="analysisDirectory"></param>
        /// <param name="dateTime"></param>
        public CrossSectionWorkflowResult(
            string datasetPath,
            IImsTarget target, 
            AnalysisStatus analysisStatus, 
            AssociationHypothesisInfo associationHypothesisInfo, 
            IList<IdentifiedIsomerInfo> isomerResults, 
            PeakScores averageObservedPeakStatistics, 
            double averageVoltageGroupStability, 
            string analysisDirectory, 
            string dateTime)
        {
            this.Target = target;
            this.AnalysisStatus = analysisStatus;
            this.AssociationHypothesisInfo = associationHypothesisInfo;
            this.isomerResults = isomerResults;
            this.AverageObservedPeakStatistics = averageObservedPeakStatistics;
            this.AverageVoltageGroupStability = averageVoltageGroupStability;
            this.AnalysisDirectory = analysisDirectory;
            this.DateTime = dateTime;
            this.DatasetPath = datasetPath;
            this.DatasetName = Path.GetFileNameWithoutExtension(datasetPath);
            this.DateTime = dateTime;
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
        /// <param name="datasetPath"></param>
        /// <returns>
        /// The <see cref="CrossSectionWorkflowResult"/>.
        /// </returns>
        public static CrossSectionWorkflowResult CreateErrorResult(IImsTarget target, string datasetName, string datasetPath, string analysisPath, string SampleCollectionTime)
        {
            // No hypothesis can be made.
            return new CrossSectionWorkflowResult(
                datasetPath,
                target, 
                AnalysisStatus.UknownError,
                null, 
                null,
                null, 
                0,
                analysisPath,
                SampleCollectionTime
                );
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
        internal static CrossSectionWorkflowResult CreateNegativeResult(IEnumerable<ObservedPeak> rejectedPeaks, IEnumerable<VoltageGroup> rejectedVoltageGroups, IImsTarget target, string datasetPath, string analysisPath, string sampleCollectionTime)
        {
            // No valid feature peaks were identified. No hypothesis.
            double voltageGroupScore = VoltageGroupScoring.ComputeAverageVoltageGroupStabilityScore(rejectedVoltageGroups);
            
            // quantize the VG score from VGs in the removal list.
            IEnumerable<PeakScores> featureStats = rejectedPeaks.Select(x => x.Statistics);
            PeakScores averagePeakScores = FeatureScoreUtilities.AverageFeatureStatistics(featureStats);
            CrossSectionWorkflowResult informedResult = new CrossSectionWorkflowResult(
                datasetPath,
                target, 
                AnalysisStatus.Negative, 
                null,
                averagePeakScores,
                voltageGroupScore,
                analysisPath,
                sampleCollectionTime);

            return informedResult;
        }

        internal static CrossSectionWorkflowResult CreateResultFromAssociationHypothesis(CrossSectionSearchParameters parameters, AssociationHypothesis optimalHypothesis, IImsTarget target, IEnumerable<VoltageGroup> allVoltageGroups, IEnumerable<ObservedPeak> allPeaks, string datasetPath, string analysisPath, string  sampleCollectionDate, double viperCompatibleMass = 0)
        {
            // Initialize the result struct.
            AssociationHypothesisInfo associationHypothesisInfo = new AssociationHypothesisInfo(optimalHypothesis.ProbabilityOfDataGivenHypothesis, optimalHypothesis.ProbabilityOfHypothesisGivenData);

            double averageVoltageGroupScore = VoltageGroupScoring.ComputeAverageVoltageGroupStabilityScore(allVoltageGroups);
            IEnumerable<PeakScores> allFeatureStatistics = allPeaks.Select(x => x.Statistics);
            PeakScores averageObservedPeakStatistics = FeatureScoreUtilities.AverageFeatureStatistics(allFeatureStatistics);

            IEnumerable<IsomerTrack> tracks = optimalHypothesis.Tracks.ToList();

            // Find the conformer with the closest m/z
            IsomerTrack trackWithBestMz = tracks.OrderBy(x => Math.Abs(Metrics.DaltonToPpm(x.AverageMzInDalton - target.MassWithAdduct, target.MassWithAdduct))).First();
            double bestMzInPpm = Metrics.DaltonToPpm(trackWithBestMz.AverageMzInDalton - target.MassWithAdduct, target.MassWithAdduct);
            IList<IdentifiedIsomerInfo> isomersInfo = tracks.Select(x => x.ExportIdentifiedIsomerInfo(viperCompatibleMass, parameters.MinFitPoints, parameters.MinR2,  target, bestMzInPpm)).ToList();
            
            AnalysisStatus finalStatus = TrackToHypothesisConclusionLogic(isomersInfo.Select(info => info.AnalysisStatus));

            CrossSectionWorkflowResult informedResult = new CrossSectionWorkflowResult(
            datasetPath,
            target,
            finalStatus, 
            associationHypothesisInfo,
            isomersInfo,
            averageObservedPeakStatistics,
            averageVoltageGroupScore,
            analysisPath,
            sampleCollectionDate
            );
            
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
