// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoleculeWorkflowParameters.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The molecule workflow parameters.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Parameters
{
    /// <summary>
    /// The molecule workflow parameters.
    /// </summary>
    public class MoleculeWorkflowParameters : InformedParameters
    {
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MoleculeWorkflowParameters"/> class.
        /// </summary>
        public MoleculeWorkflowParameters()
        {
            // default values
            this.IsotopicFitScoreMax = 0.15;
            this.MassToleranceInPpm = 10;
            this.NumPointForSmoothing = 9;
            this.ConfidenceThreshold = 0.5;
            this.FeatureFilterLevel = 0.25;
        }

        /// <summary>
        /// Gets or sets the feature filter level.
        /// </summary>
        public double FeatureFilterLevel { get; set; }

        public double ConfidenceThreshold { get; set; }
    }
}
