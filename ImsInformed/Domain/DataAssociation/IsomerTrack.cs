// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
namespace ImsInformed.Domain.DataAssociation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;

    using ImsInformed.Domain.DataAssociation.IonSignatureMatching;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Filters;
    using ImsInformed.Scoring;
    using ImsInformed.Statistics;
    using ImsInformed.Targets;
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

        public double T0;
    }

    /// <summary>
    /// The isomer trace.
    /// </summary>
    internal class IsomerTrack
    {
        /// <summary>
        /// The observed peaks.
        /// </summary>
        private readonly HashSet<ObservedPeak> observedPeaks;

        /// <summary>
        /// The drift tube length in meters.
        /// </summary>
        private readonly double driftTubeLengthInMeters;

        /// <summary>
        /// The inferedTarget.
        /// </summary>
        private readonly HashSet<VoltageGroup> definedVoltageGroups;

        /// <summary>
        /// The FitLine.
        /// </summary>
        public FitLine FitLine
        {
            get
            {
                if (this.fitLineNotComputed)
                {
                    this.ComputeLinearFitLine();
                }

                return this.fitLine;
            }
        }

        /// <summary>
        /// The observations has changed.
        /// </summary>
        private bool fitLineNotComputed;

        private int numberOfVoltageGroups;

        /// <summary>
        /// The ion signature matching probability.
        /// </summary>
        private double ionSignatureMatchingProbability;

        /// <summary>
        /// The mobility info.
        /// </summary>
        private MobilityInfo mobilityInfo;

        /// <summary>
        /// The fit line.
        /// </summary>
        private FitLine fitLine;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsomerTrack"/> class.
        /// </summary>
        /// <param name="peaks">
        /// The peaks.
        /// </param>
        /// <param name="driftTubeLengthInMeters">
        /// The drift tube length in meters.
        /// </param>
        public IsomerTrack(IEnumerable<ObservedPeak> peaks, double driftTubeLengthInMeters, int numberOfVoltageGroups) : this(driftTubeLengthInMeters, numberOfVoltageGroups)
        {
            foreach (var peak in peaks)
            {
                this.AddObservation(peak);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IsomerTrack"/> class.
        /// </summary>
        /// <param name="driftTubeLengthInMeters">
        /// The drift Tube Length In Meters.
        /// </param>
        /// <param name="target">
        /// The inferedTarget.
        /// </param>
        public IsomerTrack(double driftTubeLengthInMeters, int numberOfVoltageGroups)
        {
            this.driftTubeLengthInMeters = driftTubeLengthInMeters;
            this.fitLineNotComputed = true;
            this.observedPeaks = new HashSet<ObservedPeak>();
            this.definedVoltageGroups = new HashSet<VoltageGroup>();
            this.mobilityInfo.CollisionCrossSectionArea = 0;
            this.mobilityInfo.Mobility = 0;
            this.mobilityInfo.RSquared = 0;
            this.ionSignatureMatchingProbability = 0;
            this.numberOfVoltageGroups = numberOfVoltageGroups;
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
        /// Gets the Pr(Tk)
        /// </summary>
        public double TrackProbability
        {
            get
            {
                // R2 should play more of a role in determining track probability. So higher weight is given.

                double pR2 = this.FitLine.RSquared;
                double digits = 0 - Math.Log10(1- pR2);
                pR2 = ScoreUtil.MapToZeroOneExponential(digits, 2, 0.9);
                double pSig = this.ionSignatureMatchingProbability;
                double pcount = ScoreUtil.MapToZeroOneExponential(this.RealPeakCount, (double)this.numberOfVoltageGroups * 0.9, 0.9);

                double r2Weight = DataAssociationTuningParameters.R2Weight;
                double peakCountWeight = DataAssociationTuningParameters.PeakCountWeight;
                double signatureConsistencyWeight = DataAssociationTuningParameters.SignatureConsistencyWeight;
                double sum = r2Weight + signatureConsistencyWeight + peakCountWeight;
                r2Weight = r2Weight / sum;
                signatureConsistencyWeight = signatureConsistencyWeight / sum;
                peakCountWeight = peakCountWeight / sum;

                double logResult = r2Weight * Math.Log(pR2) + 
                signatureConsistencyWeight * Math.Log(pSig) +
                peakCountWeight * Math.Log(pcount);

                double result = Math.Exp(logResult);

                return result;
            }
        }

        public PeakScores TrackStatistics
        {
            get 
            {
                IEnumerable<PeakScores> allFeatureStatistics = this.ObservedPeaks.Select(x => x.Statistics);
                PeakScores averageObservedPeakStatistics = FeatureScoreUtilities.AverageFeatureStatistics(allFeatureStatistics);
                return averageObservedPeakStatistics;
            }
        }

        /// <summary>
        /// Gets the real peak count.
        /// </summary>
        public int RealPeakCount
        {
            get
            {
                int count = 0;
                foreach (var peak in this.ObservedPeaks)
                {
                    if (peak.Peak != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    
        /// <summary>
        /// Gets the real peak count.
        /// </summary>
        public string TrackDescriptor
        {
            get
            {
                string result = string.Format("R2: {0:F4}: ", this.FitLine.RSquared);
                foreach (ObservedPeak observation in this.ObservedPeaks)
                {
                    result += observation;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the real peak count.
        /// </summary>
        public double AverageMzInDalton
        {
            get
            {
                double mzAverage = 0;
                int peakCount = this.RealPeakCount;
                foreach (var peak in this.ObservedPeaks)
                {
                    mzAverage += peak.Peak.PeakApex.MzCenterInDalton;
                }

                return mzAverage / peakCount;
            }
        }

        /// <summary>
        /// Gets the mobility info for inferedTarget
        /// </summary>
        /// <param name="inferedTarget">
        /// The inferedTarget.
        /// </param>
        public MobilityInfo GetMobilityInfoForTarget(IImsTarget inferedTarget)
        {
            return this.ComputeMobilityInfo(inferedTarget);
        }

        public void AddIonTransition(IonTransition transition)
        {
            if (this.ionSignatureMatchingProbability == 0)
            {
                this.ionSignatureMatchingProbability = transition.TransitionProbability;
            }
            else
            {
                this.ionSignatureMatchingProbability *= transition.TransitionProbability;
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
            this.fitLineNotComputed = true;
            
            if (!this.definedVoltageGroups.Contains(peak.VoltageGroup))
            {
                this.definedVoltageGroups.Add(peak.VoltageGroup);
            }
            else
            {
                throw new ArgumentException("Voltage group is already defined track");
            }
        }

        /// <summary>
        /// The get drift time error in seconds for obsevation.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double GetDriftTimeErrorInSecondsForObsevation(ObservedPeak peak)
        {
            ContinuousXYPoint observationPoint = peak.ToContinuousXyPoint();
            double actualDriftTimeInSeconds = observationPoint.Y / 1000;
            double predictedDriftTimeInSeconds = this.fitLine.ModelPredictX2Y(observationPoint.X) / 1000;
            double error = Math.Abs(actualDriftTimeInSeconds - predictedDriftTimeInSeconds);
            return error;
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
        /// <param name="target"></param>
        /// <returns>
        /// The <see cref="IdentifiedIsomerInfo"/>.
        /// </returns>
        public IdentifiedIsomerInfo ExportIdentifiedIsomerInfo(double viperCompatibleMass, int minFitPoints, double minR2, IImsTarget target, double bestMzInPpm)
        {
            double averageVoltageGroupStabilityScore = VoltageGroupScoring.ComputeAverageVoltageGroupStabilityScore(this.definedVoltageGroups);

            IList<ArrivalTimeSnapShot> snapshots = new List<ArrivalTimeSnapShot>();
            foreach (var observation in this.observedPeaks)
            {
                snapshots.Add(this.ExtractArrivalTimeSnapShot(observation));
            }

            IdentifiedIsomerInfo info = new IdentifiedIsomerInfo(
                this.observedPeaks.Count,
                this.AverageMzInDalton,
                this.mobilityInfo.RSquared,
                this.mobilityInfo.Mobility,
                this.mobilityInfo.CollisionCrossSectionArea,
                averageVoltageGroupStabilityScore,
                snapshots,
                viperCompatibleMass,
                this.ConcludeStatus(minFitPoints, minR2),
                this.TrackStatistics,
                target,
                this.mobilityInfo.T0,
                bestMzInPpm
                );
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
            TrackFilter filter = new TrackFilter();
            bool lowFitPoints = filter.FilterLowFitPointNumber(this.ObservedPeaks.Count(), minFitPoints);
            bool lowR2 = filter.IsLowR2(this.mobilityInfo.RSquared, minR2);

            return lowFitPoints ? AnalysisStatus.NotSufficientPoints : 
                lowR2 ? AnalysisStatus.Rejected : AnalysisStatus.Positive;
        }

        /// <summary>
        /// The compute linear fit FitLine.
        /// </summary>
        private void ComputeLinearFitLine()
        {
            IEnumerable<ContinuousXYPoint> points = this.ToContinuousXyPoints();
            this.fitLine = new LeastSquaresFitLine(points);
            this.fitLine.PerformRegression();
            this.fitLine.DiagnoseRegression();
            this.fitLineNotComputed = false;
        }

        /// <summary>
        /// The compute mobility info.
        /// </summary>
        /// <param name="target">
        /// The inferred target.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private MobilityInfo ComputeMobilityInfo(IImsTarget target)
        {
            if (this.fitLineNotComputed)
            {
                this.ComputeLinearFitLine();
            }

            // Convert the track into a Continuous XY data points.
            this.mobilityInfo.Mobility = this.driftTubeLengthInMeters * this.driftTubeLengthInMeters * 1000 / (this.FitLine.Slope);
            this.mobilityInfo.RSquared = this.FitLine.RSquared;
            this.mobilityInfo.T0 = this.FitLine.Intercept;
            
            Composition bufferGas = new Composition(0, 0, 2, 0, 0);
            double reducedMass = MoleculeUtil.ComputeReducedMass(target.MassWithAdduct, bufferGas);
            double meanTemperatureInKelvin = this.ComputeGlobalMeanTemperature();
            this.mobilityInfo.CollisionCrossSectionArea = MoleculeUtil.ComputeCrossSectionalArea(
                meanTemperatureInKelvin,
                this.mobilityInfo.Mobility,
                target.ChargeState, 
                reducedMass);
            
            return this.mobilityInfo;
        }

        /// <summary>
        /// The extract arrival time snap shot.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <returns>
        /// The <see cref="ArrivalTimeSnapShot"/>.
        /// </returns>
        private ArrivalTimeSnapShot ExtractArrivalTimeSnapShot(ObservedPeak peak)
        {
            ArrivalTimeSnapShot snapShot;
            snapShot.MeasuredArrivalTimeInMs = peak.Peak.PeakApex.DriftTimeCenterInMs;
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
                double voltageGroupTemperature = Metrics.AbsoluteZeroInKelvin * group.MeanTemperatureNondimensionalized;
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
        private IEnumerable<ContinuousXYPoint> ToContinuousXyPoints()
        {
            HashSet<ContinuousXYPoint> allFitPoints = new HashSet<ContinuousXYPoint>();
            foreach (ObservedPeak observation in this.observedPeaks)
            {
                ContinuousXYPoint point = observation.ToContinuousXyPoint();
                allFitPoints.Add(point);
            }

            return allFitPoints;
        }
    }
}
