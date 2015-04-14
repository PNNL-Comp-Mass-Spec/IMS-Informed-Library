// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TargetType.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The Target type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    /// <summary>
    /// The Target type.
    /// </summary>
    public enum TargetType
    {
        /// <summary>
        /// The peptide, characterized by amino acid sequence
        /// </summary>
        Peptide,

        /// <summary>
        /// The small molecule, characterized by empirical formula
        /// </summary>
        Molecule
    }
}
