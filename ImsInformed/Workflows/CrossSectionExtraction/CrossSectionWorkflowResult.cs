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
    using ImsInformed.Interfaces;
    using ImsInformed.Scoring;
    using ImsInformed.Targets;

    /// <summary>
    /// The molecule informed workflow result.
    /// </summary>
    [Serializable]
    public class CrossSectionWorkflowResult
    {
        /// <summary>
        /// The dataset name.
        /// </summary>
        public readonly string DatasetName;

        /// <summary>
        /// The Target.
        /// </summary>
        public readonly IImsTarget Target;

        /// <summary>
        /// The analysis status.
        /// </summary>
        public readonly AnalysisStatus AnalysisStatus;

        /// <summary>
        /// The r squared.
        /// </summary>
        public readonly AnalysisScoresHolder AnalysisScoresHolder;

        /// <summary>
        /// The isomer results.
        /// </summary>
        private readonly IEnumerable<TargetIsomerReport> isomerResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionWorkflowResult"/> class. 
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
        /// <param name="analysisScoresHolder">
        /// The analysis scores holder.
        /// </param>
        /// <param name="isomerResults">
        /// The isomer results.
        /// </param>
        public CrossSectionWorkflowResult(string datasetName, IImsTarget target, AnalysisStatus analysisStatus, AnalysisScoresHolder analysisScoresHolder, IEnumerable<TargetIsomerReport> isomerResults)
        {
            this.DatasetName = datasetName;
            this.Target = target;
            this.AnalysisStatus = analysisStatus;
            this.AnalysisScoresHolder = analysisScoresHolder;
            this.isomerResults = isomerResults;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionWorkflowResult"/> class.
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
        /// <param name="analysisScoresHolder">
        /// The analysis scores holder.
        /// </param>
        /// <param name="isomerResult">
        /// The isomer result.
        /// </param>
        public CrossSectionWorkflowResult(
            string datasetName,
            IImsTarget target,
            AnalysisStatus analysisStatus,
            AnalysisScoresHolder analysisScoresHolder,
            TargetIsomerReport isomerResult)
            : this(
                datasetName,
                target,
                analysisStatus,
                analysisScoresHolder,
                new List<TargetIsomerReport> { isomerResult })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionWorkflowResult"/> class.
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
        /// <param name="analysisScoresHolder">
        /// The analysis scores holder.
        /// </param>
        public CrossSectionWorkflowResult(
            string datasetName,
            IImsTarget target,
            AnalysisStatus analysisStatus,
            AnalysisScoresHolder analysisScoresHolder)
            : this(
                datasetName,
                target,
                analysisStatus,
                analysisScoresHolder,
                new List<TargetIsomerReport>())
        {
        }


        /// <summary>
        /// The isomer results.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public IEnumerable<TargetIsomerReport> MatchingIsomers
        {
            get 
            {
                return this.isomerResults.ToList();
            }
        }
    }
}
