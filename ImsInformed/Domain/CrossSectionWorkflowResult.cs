// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrossSectionInformedWorkflowResult.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the CrossSectionInformedWorkflowResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using ImsInformed.Scoring;

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
        /// TargetDescriptor, either the empirical formula or the MZ used.
        /// </summary>
        public readonly string TargetDescriptor;

        /// <summary>
        /// The ionization method.
        /// </summary>
        public readonly IonizationMethod IonizationMethod;

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
        /// Initializes a new instance of the <see cref="MoleculeInformedWorkflowResult"/> class.
        /// </summary>
        /// <param name="datasetName">
        /// The dataset name.
        /// </param>
        /// <param name="targetDescriptor">
        /// The target descriptor.
        /// </param>
        /// <param name="ionizationMethod">
        /// The ionization method.
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
        public CrossSectionWorkflowResult(string datasetName, string targetDescriptor, IonizationMethod ionizationMethod, AnalysisStatus analysisStatus, AnalysisScoresHolder analysisScoresHolder, IEnumerable<TargetIsomerReport> isomerResults)
        {
            this.DatasetName = datasetName;
            this.TargetDescriptor = targetDescriptor;
            this.IonizationMethod = ionizationMethod;
            this.AnalysisStatus = analysisStatus;
            this.AnalysisScoresHolder = analysisScoresHolder;
            this.isomerResults = isomerResults;
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
