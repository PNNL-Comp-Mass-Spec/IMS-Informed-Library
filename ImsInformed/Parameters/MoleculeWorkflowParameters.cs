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
            this.MassToleranceInPpm = 10;
            this.NumPointForSmoothing = 9;
            this.FeatureFilterLevel = 0.25;
            this.IntensityThreshold = 0.4;
            this.PeakShapeThreshold = 0.4;
            this.IsotopicFitScoreThreshold = 0.4;
            this.MinFitPoints = 4;
        }

        /// <summary>
        /// Gets or sets the feature filter level.
        /// </summary>
        public double FeatureFilterLevel { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double IntensityThreshold { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double PeakShapeThreshold { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double IsotopicThreshold { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold.
        /// </summary>
        public double MinFitPoints { get; set; }
    }
}
