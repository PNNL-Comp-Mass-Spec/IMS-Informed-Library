// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IonSignatureDistance.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the IonSignatureDistance type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DirectInjection
{
    using System;

    /// <summary>
    /// The ion signature distance.
    /// </summary>
    public class IonSignatureDistance : IComparable 
    {
        /// <summary>
        /// The mz center difference.
        /// </summary>
        public readonly double MzCenterDifference;

        /// <summary>
        /// The intensity distrubution ssd.
        /// </summary>
        public readonly double IntensityDistrubutionSsd;

        /// <summary>
        /// Initializes a new instance of the <see cref="IonSignatureDistance"/> class.
        /// </summary>
        /// <param name="mzCenterDifference">
        /// The mz center difference.
        /// </param>
        /// <param name="intensityDistributionSSD">
        /// The intensity distribution ssd.
        /// </param>
        public IonSignatureDistance(double mzCenterDifference, double intensityDistributionSSD)
        {
            this.MzCenterDifference = mzCenterDifference;
            this.IntensityDistrubutionSsd = intensityDistributionSSD;
        }

        public int CompareTo(IonSignatureDistance other)
        {
            return this.IonSignatureSSD().CompareTo(other.IonSignatureSSD());
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            IonSignatureDistance otherTemperature = obj as IonSignatureDistance;
            if (otherTemperature != null) 
            {
                return this.CompareTo(otherTemperature);
            }
            else 
            {
               throw new ArgumentException("Object is not a Temperature");
            }
        }

        public double IonSignatureSSD()
        {
            return this.MzCenterDifference * this.MzCenterDifference + this.IntensityDistrubutionSsd * this.IntensityDistrubutionSsd;
        }
    }
}
