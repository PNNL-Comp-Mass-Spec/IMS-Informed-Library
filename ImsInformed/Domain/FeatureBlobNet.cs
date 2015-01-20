// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureBlobNet.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the FeatureBlobNet type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// The feature blob net.
    /// </summary>
    public class FeatureBlobNet
    {
        /// <summary>
        /// Gets the feature blob.
        /// </summary>
        public FeatureBlob FeatureBlob { get; private set; }

        /// <summary>
        /// Gets the normalized elution time.
        /// </summary>
        public double NormalizedElutionTime { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureBlobNet"/> class.
        /// </summary>
        /// <param name="featureBlob">
        /// The feature blob.
        /// </param>
        /// <param name="normalizedElutionTime">
        /// The normalized elution time.
        /// </param>
        public FeatureBlobNet(FeatureBlob featureBlob, double normalizedElutionTime)
        {
            this.FeatureBlob = featureBlob;
            this.NormalizedElutionTime = normalizedElutionTime;
        }
    }
}
