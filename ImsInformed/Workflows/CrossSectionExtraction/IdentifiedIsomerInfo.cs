// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IdentifiedIsomerInfo.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The isomer result.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using System;
    using System.Collections.Generic;

    using ImsInformed.Domain;
    using ImsInformed.Scoring;
    using ImsInformed.Targets;

    /// <summary>
    ///     The isomer result.
    /// </summary>
    [Serializable]
    public class IdentifiedIsomerInfo
    {
        /// <summary>
        ///     The monoisotopic mass.
        /// </summary>
        public readonly double ViperCompatibleMass;

        /// <summary>
        ///     The cross sectional area.
        /// </summary>
        public readonly IEnumerable<ArrivalTimeSnapShot> ArrivalTimeSnapShots;

        /// <summary>
        ///     Gets the average voltage group stability score.
        /// </summary>
        public readonly double AverageVoltageGroupStabilityScore;

        /// <summary>
        ///     The cross sectional area.
        /// </summary>
        public readonly double CrossSectionalArea;

        /// <summary>
        ///     The normalized mobility K0
        /// </summary>
        public readonly double Mobility;

        /// <summary>
        ///     The time the ion spend outside the drift tube
        /// </summary>
        public readonly double T0;

        public readonly double MzInDalton;

        public readonly double MzInPpm;

        /// <summary>
        ///     The points used.
        /// </summary>
        public readonly int NumberOfFeaturePointsUsed;

        /// <summary>
        ///     The r squared.
        /// </summary>
        public readonly double RSquared;

        /// <summary>
        /// The track status.
        /// </summary>
        public readonly AnalysisStatus AnalysisStatus;

        /// <summary>
        /// The track status.
        /// </summary>
        public readonly PeakScores PeakScores;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentifiedIsomerInfo"/> class.
        /// </summary>
        /// <param name="numberOfFeaturePointsUsed">
        /// The number of feature points used.
        /// </param>
        /// <param name="mzInDalton"></param>
        /// <param name="rSquared">
        /// The r squred.
        /// </param>
        /// <param name="mobility">
        /// The mobility.
        /// </param>
        /// <param name="crossSectionalArea">
        /// The cross sectional area.
        /// </param>
        /// <param name="averageVoltageGroupStabilityScore">
        /// The average voltage group stability score.
        /// </param>
        /// <param name="arrivalTimeSnapShots">
        /// The arrival time snap shots.
        /// </param>
        /// <param name="viperCompatibleMass">
        /// The viper Compatible Mass.
        /// </param>
        /// <param name="analysisStatus"></param>
        /// <param name="peakScores"></param>
        /// <param name="target"></param>
        public IdentifiedIsomerInfo(
            int numberOfFeaturePointsUsed,
            double mzInDalton,
            double rSquared, 
            double mobility, 
            double crossSectionalArea, 
            double averageVoltageGroupStabilityScore, 
            IEnumerable<ArrivalTimeSnapShot> arrivalTimeSnapShots, 
            double viperCompatibleMass, 
            AnalysisStatus analysisStatus, 
            PeakScores peakScores,
            IImsTarget target,
            double t0)
        {
            this.NumberOfFeaturePointsUsed = numberOfFeaturePointsUsed;
            this.RSquared = rSquared;
            this.Mobility = mobility;
            this.CrossSectionalArea = crossSectionalArea;
            this.AverageVoltageGroupStabilityScore = averageVoltageGroupStabilityScore;
            this.ArrivalTimeSnapShots = arrivalTimeSnapShots;
            this.ViperCompatibleMass = viperCompatibleMass;
            this.AnalysisStatus = analysisStatus;
            this.PeakScores = peakScores;
            this.T0 = t0;
            this.MzInDalton = mzInDalton;
            this.MzInPpm = Util.Metrics.DaltonToPpm(mzInDalton - target.MassWithAdduct, target.MassWithAdduct);
        }
    }
}