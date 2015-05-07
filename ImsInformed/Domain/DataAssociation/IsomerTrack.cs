// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsomerTrack.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the IsomerTrack type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation
{
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Filters;
    using ImsInformed.Interfaces;
    using ImsInformed.Scoring;
    using ImsInformed.Stats;
    using ImsInformed.Util;
    using ImsInformed.Workflows.CrossSectionExtraction;

    using InformedProteomics.Backend.Data.Composition;

    /// <summary>
    /// The mobility info.
    /// </summary>
    public struct MobilityInfo
    {
        /// <summary>
        /// The mobility.
        /// </summary>
        public double Mobility;

        /// <summary>
        /// The r squared.
        /// </summary>
        public double RSquared;

        /// <summary>
        /// The collision cross section area.
        /// </summary>
        public double CollisionCrossSectionArea;
    }

    /// <summary>
    /// The isomer trace.
    /// </summary>
    public class IsomerTrack
    {
        /// <summary>
        /// The observed peaks.
        /// </summary>
        private readonly IList<ObservedPeak> observedPeaks;

        /// <summary>
        /// The drift tube length in meters.
        /// </summary>
        private readonly double driftTubeLengthInMeters;

        /// <summary>
        /// The target.
        /// </summary>
        private readonly IImsTarget target;

        /// <summary>
        /// The target.
        /// </summary>
        private readonly HashSet<VoltageGroup> definedVoltageGroups;

        /// <summary>
        /// The line.
        /// </summary>
        private FitLine line;

        /// <summary>
        /// The observations has changed.
        /// </summary>
        private bool observationsHasChanged;

        /// <summary>
        /// The mobility info.
        /// </summary>
        private MobilityInfo mobilityInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsomerTrack"/> class.
        /// </summary>
        /// <param name="driftTubeLengthInMeters">
        /// The drift Tube Length In Meters.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        public IsomerTrack(double driftTubeLengthInMeters, IImsTarget target)
        {
            this.target = target;
            this.driftTubeLengthInMeters = driftTubeLengthInMeters;
            this.observationsHasChanged = true;
            this.observedPeaks = new List<ObservedPeak>();
            this.definedVoltageGroups = new HashSet<VoltageGroup>();
            this.mobilityInfo.CollisionCrossSectionArea = 0;
            this.mobilityInfo.Mobility = 0;
            this.mobilityInfo.RSquared = 0;
        }

        /// <summary>
        /// Gets the observed peaks.
        /// </summary>
        public IEnumerable<ObservedPeak> ObservedPeaks
        {
            get
            {
                return this.observedPeaks;
            }
        }

        /// <summary>
        /// Gets the mobility info.
        /// </summary>
        public MobilityInfo MobilityInfo
        {
            get
            {
                if (this.observationsHasChanged)
                {
                    return this.ComputeMobilityInfo();
                }

                return this.mobilityInfo;
            }
        }

        /// <summary>
        /// The add observation.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public void AddObservation(ObservedPeak peak)
        {
            this.observedPeaks.Add(peak);
            this.observationsHasChanged = true;
            
            if (!this.definedVoltageGroups.Contains(peak.VoltageGroup))
            {
                this.definedVoltageGroups.Add(peak.VoltageGroup);
            }
        }

        /// <summary>
        /// The export identified isomer info.
        /// </summary>
        /// <param name="viperCompatibleMass">
        /// The viper compatible mass.
        /// </param>
        /// <param name="minFitPoints">
        /// The min fit points.
        /// </param>
        /// <param name="minR2">
        /// The min r 2.
        /// </param>
        /// <returns>
        /// The <see cref="IdentifiedIsomerInfo"/>.
        /// </returns>
        public IdentifiedIsomerInfo ExportIdentifiedIsomerInfo(double viperCompatibleMass, int minFitPoints, double minR2)
        {
            double averageVoltageGroupStabilityScore = VoltageGroupScore.ComputeAverageVoltageGroupStabilityScore(this.definedVoltageGroups);

            IList<ArrivalTimeSnapShot> snapshots = new List<ArrivalTimeSnapShot>();
            foreach (var observation in this.observedPeaks)
            {
                snapshots.Add(this.ExtractArrivalTimeSnapShot(observation));
            }

            IdentifiedIsomerInfo info = new IdentifiedIsomerInfo(
                this.observedPeaks.Count,
                this.mobilityInfo.RSquared,
                this.mobilityInfo.Mobility,
                this.mobilityInfo.CollisionCrossSectionArea,
                averageVoltageGroupStabilityScore,
                snapshots,
                viperCompatibleMass,
                this.ConcludeStatus(minFitPoints, minR2));
            return info;
        }

        /// <summary>
        /// The conclude status.
        /// </summary>
        /// <param name="minFitPoints">
        /// The min fit points.
        /// </param>
        /// <param name="minR2">
        /// The min r 2.
        /// </param>
        /// <returns>
        /// The <see cref="AnalysisStatus"/>.
        /// </returns>
        private AnalysisStatus ConcludeStatus(int minFitPoints, double minR2)
        {
            bool lowFitPoints = AnalysisFilter.FilterLowFitPointNumber(this.ObservedPeaks.Count(), minFitPoints);
            bool lowR2 = AnalysisFilter.FilterLowR2(this.mobilityInfo.RSquared, minR2);

            return lowFitPoints ? AnalysisStatus.NotSufficientPoints : 
                lowR2 ? AnalysisStatus.Rejected : AnalysisStatus.Positive;
        }

        /// <summary>
        /// The compute mobility info.
        /// </summary>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private MobilityInfo ComputeMobilityInfo()
        {
            // Convert the track into a Continuous XY data points.
            IEnumerable<ContinuousXYPoint> points = this.ToContinuousXyPoint();
            this.line = new FitLine(points);
            this.mobilityInfo.Mobility = this.driftTubeLengthInMeters * this.driftTubeLengthInMeters / (1 / this.line.Slope);
            this.mobilityInfo.RSquared = this.line.RSquared;
            
            Composition bufferGas = new Composition(0, 0, 2, 0, 0);
            double reducedMass = MoleculeUtil.ComputeReducedMass(this.target.MassWithAdduct, bufferGas);
            double meanTemperatureInKelvin = this.ComputeGlobalMeanTemperature();
            this.mobilityInfo.CollisionCrossSectionArea = MoleculeUtil.ComputeCrossSectionalArea(
                meanTemperatureInKelvin,
                this.mobilityInfo.Mobility,
                this.target.ChargeState, 
                reducedMass);

            this.observationsHasChanged = false;
            return this.mobilityInfo;
        }

        private ArrivalTimeSnapShot ExtractArrivalTimeSnapShot(ObservedPeak peak)
        {
            ArrivalTimeSnapShot snapShot;
            snapShot.MeasuredArrivalTimeInMs = peak.Peak.HighestPeakApex.DriftTimeCenterInMs;
            snapShot.PressureInTorr = peak.VoltageGroup.MeanPressureInTorr;
            snapShot.TemperatureInKelvin = peak.VoltageGroup.MeanTemperatureInKelvin;
            snapShot.DriftTubeVoltageInVolt = peak.VoltageGroup.MeanVoltageInVolts;
            return snapShot;
        }

        /// <summary>
        /// The compute global mean temperature.
        /// </summary>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private double ComputeGlobalMeanTemperature()
        {
            // Find the average temperature across various non outlier voltage groups.
            double globalMeanTemperature = 0;
            int frameCount = 0;
            foreach (VoltageGroup group in this.observedPeaks.Select(peak => peak.VoltageGroup))
            {
                double voltageGroupTemperature = UnitConversion.AbsoluteZeroInKelvin * group.MeanTemperatureNondimensionalized;
                globalMeanTemperature += voltageGroupTemperature * group.FrameAccumulationCount;
                frameCount += group.FrameAccumulationCount;
            }
            
            globalMeanTemperature /= frameCount;
            
            return globalMeanTemperature;
        }

        /// <summary>
        /// The to continuous XY point.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        private IEnumerable<ContinuousXYPoint> ToContinuousXyPoint()
        {
            // Calculate the fit line from the remaining voltage groups with reliable drift time measurement.
            HashSet<ContinuousXYPoint> allFitPoints = new HashSet<ContinuousXYPoint>();
            foreach (ObservedPeak observation in this.observedPeaks)
            {
                VoltageGroup voltageGroup = observation.VoltageGroup;
                StandardImsPeak peak = observation.Peak;

                // convert drift time to SI unit seconds
                double x = peak.HighestPeakApex.DriftTimeCenterInMs / 1000;
            
                // P/(T*V) value in pascal per (volts * kelvin)
                double y = voltageGroup.MeanPressureNondimensionalized / voltageGroup.MeanVoltageInVolts
                           / voltageGroup.MeanTemperatureNondimensionalized;
                 
                ContinuousXYPoint point = new ContinuousXYPoint(x, y);
            
                allFitPoints.Add(point);
            }

            return allFitPoints;
        }
    }
}
