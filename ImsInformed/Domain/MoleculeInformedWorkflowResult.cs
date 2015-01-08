// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoleculeInformedWorkflowResult.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the MoleculeInformedWorkflowResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    using System;

    /// <summary>
    /// The molecule informed workflow result.
    /// </summary>
    [Serializable]
    public struct MoleculeInformedWorkflowResult
    {
        /// <summary>
        /// The dataset name.
        /// </summary>
        public string DatasetName;

        /// <summary>
        /// TargetDescriptor, either the empirical formula or the MZ used.
        /// </summary>
        public string TargetDescriptor;

        /// <summary>
        /// The ionization method.
        /// </summary>
        public IonizationMethod IonizationMethod;

        /// <summary>
        /// The analysis status.
        /// </summary>
        public AnalysisStatus AnalysisStatus;

        /// <summary>
        /// The mobility.
        /// </summary>
        public double Mobility;

        /// <summary>
        /// The cross sectional area.
        /// </summary>
        public double CrossSectionalArea;

        /// <summary>
        /// The r squared.
        /// </summary>
        public double RSquared;
    }
}
