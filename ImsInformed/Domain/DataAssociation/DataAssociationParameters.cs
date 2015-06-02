// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataAssociationParameters.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The ion association tunning.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation
{
    using System;

    /// <summary>
    /// The ion association tunning class
    /// </summary>
    [Serializable]
    internal class DataAssociationParameters
    {
        /// <summary>
        /// The intensity weight.
        /// </summary>
        public const double IntensityWeight = 2;

        /// <summary>
        /// The diffusion profile weight.
        /// </summary>
        public const double DiffusionProfileWeight = 1;

        /// <summary>
        /// The M/Z match weight.
        /// </summary>
        public const double MzMatchWeight = 3;

        /// <summary>
        /// The mz difference in ppm 09.
        /// </summary>
        public const double MzDifferenceInPpm09 = 30;

        /// <summary>
        /// The mz difference in ppm 09.
        /// </summary>
        public const double DriftTimeDifferenceInMs09 = 0.1;

        /// <summary>
        /// An outlier's Pr(xi | T)
        /// </summary>
        public const double PxTOutlier = 0.80;

        /// <summary>
        /// An outlier's Pr(xi | T)
        /// </summary>
        public const double PxTInlier = 1;
    }
}
