// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrackFilter.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The track filter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Filters
{
    using ImsInformed.Domain.DataAssociation;
    using ImsInformed.Targets;
    using ImsInformed.Workflows.CrossSectionExtraction;

    /// <summary>
    /// The track filter.
    /// </summary>
    internal class TrackFilter
    {
                /// <summary>
        /// The is track possible.
        /// </summary>
        /// <param name="track">
        /// The track.
        /// </param>
        /// <param name="target"></param>
        /// <param name="crossSectionSearchParameters"></param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsTrackPossible(IsomerTrack track, IImsTarget target, CrossSectionSearchParameters crossSectionSearchParameters)
        {
            if (this.FilterLowFitPointNumber(track.RealPeakCount, crossSectionSearchParameters.MinFitPoints))
            {
                return false;
            }

            MobilityInfo trackMobilityInfo = track.GetMobilityInfoForTarget(target);
            if (!this.IsConsistentWithIonDynamics(trackMobilityInfo))
            {
                return false;
            }

            if (this.IsLowR2(trackMobilityInfo.RSquared, crossSectionSearchParameters.MinR2))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// The is low r 2.
        /// </summary>
        /// <param name="r2">
        /// The r 2.
        /// </param>
        /// <param name="r2Threshold">
        /// The r 2 threshold.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsLowR2(double r2, double r2Threshold = 0.9)
        {
            return r2 < r2Threshold;
        }

        /// <summary>
        /// The filter low fit point number.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <param name="minPoints">
        /// The min points.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool FilterLowFitPointNumber(int points, int minPoints)
        {
            return points < minPoints;
        }

                /// <summary>
        /// The is consistent with ion dynamics.
        /// </summary>
        /// <param name="info">
        /// The info.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsConsistentWithIonDynamics(MobilityInfo info)
        {
            if (info.Mobility < 0)
            {
                return false;
            }
            
            return true;
        }
    }
}
