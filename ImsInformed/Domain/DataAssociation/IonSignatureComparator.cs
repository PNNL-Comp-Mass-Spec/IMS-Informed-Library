// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IonSignatureComparator.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The ion signature comparator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation
{
    using System;

    using ImsInformed.Domain.DirectInjection;

    // Compare the ion signatures of two IMS observations, usually at different voltage groups
    /// <summary>
    /// The ion signature comparator.
    /// </summary>
    public class IonSignatureComparator
    {
        /// <summary>
        /// The compare ion signatures.
        /// </summary>
        /// <param name="A">
        /// The a.
        /// </param>
        /// <param name="B">
        /// The b.
        /// </param>
        /// <returns>
        /// The <see cref="IonSignatureDistance"/>.
        /// </returns>
        public IonSignatureDistance CompareIonSignatures(ObservedPeak A, ObservedPeak B)
        {
            double mzDifference = Math.Abs(A.Peak.HighestPeakApex.MzCenterInDalton - B.Peak.HighestPeakApex.MzCenterInDalton);
            double intensityDistrubutionInSSD = Math.Abs(A.Statistics.IntensityScore - B.Statistics.IntensityScore);
            return new IonSignatureDistance(mzDifference, intensityDistrubutionInSSD);
        }

        private double IonDiffusionProfileDistance (StandardImsPeak A, StandardImsPeak B)
        {
            double diffusionProfileDistance = Math.Abs(A.HighestPeakApex.MzCenterInDalton - B.HighestPeakApex.MzCenterInDalton);
            return 0;
        }
    }
}
