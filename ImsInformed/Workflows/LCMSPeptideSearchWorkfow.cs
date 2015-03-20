// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LCMSPeptideSearchWorkfow.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the LCMSPeptideSearchWorkfow type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    using DeconTools.Backend;
    using DeconTools.Backend.Core;
    using DeconTools.Backend.ProcessingTasks.FitScoreCalculators;
    using DeconTools.Backend.ProcessingTasks.PeakDetectors;
    using DeconTools.Backend.ProcessingTasks.ResultValidators;
    using DeconTools.Backend.ProcessingTasks.TargetedFeatureFinders;
    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;

    using ImsInformed.Domain;
    using ImsInformed.Parameters;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

    using MathNet.Numerics.Interpolation;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakCorrelation;
    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    /// <summary>
    /// The informed workflow.
    /// </summary>
    public class LCMSPeptideSearchWorkfow
    {
        /// <summary>
        /// The _uimf reader.
        /// </summary>
        public readonly DataReader _uimfReader;

        /// <summary>
        /// The _smoother.
        /// </summary>
        protected readonly SavitzkyGolaySmoother _smoother;

        /// <summary>
        /// The _ms feature finder.
        /// </summary>
        protected readonly IterativeTFF _msFeatureFinder;

        /// <summary>
        /// The _theoretical feature generator.
        /// </summary>
        protected readonly ITheorFeatureGenerator _theoreticalFeatureGenerator;

        /// <summary>
        /// The _left of mono peak looker.
        /// </summary>
        protected readonly LeftOfMonoPeakLooker _leftOfMonoPeakLooker;

        /// <summary>
        /// The _peak detector.
        /// </summary>
        protected readonly ChromPeakDetector _peakDetector;

        /// <summary>
        /// The _isotopic peak fit score calculator.
        /// </summary>
        protected readonly PeakLeastSquaresFitter _isotopicPeakFitScoreCalculator;

        /// <summary>
        /// The _net alignment.
        /// </summary>
        private readonly IInterpolation _netAlignment;

        /// <summary>
        /// The _parameters.
        /// </summary>
        public readonly InformedParameters _parameters;

        /// <summary>
        /// The number of frames.
        /// </summary>
        public readonly double NumberOfFrames;

        /// <summary>
        /// The number of scans.
        /// </summary>
        public readonly double NumberOfScans;

        protected Stopwatch _buildWatershedStopWatch;

        /// <summary>
        /// The _smooth stopwatch.
        /// </summary>
        protected Stopwatch _smoothStopwatch;

        /// <summary>
        /// The _feature find stop watch.
        /// </summary>
        protected Stopwatch _featureFindStopWatch;

        /// <summary>
        /// The _uimf file location.
        /// </summary>
        protected string _uimfFileLocation;

        /// <summary>
        /// The _feature find count.
        /// </summary>
        protected double _featureFindCount;

        /// <summary>
        /// The _point count.
        /// </summary>
        protected double _pointCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="LCMSPeptideSearchWorkfow"/> class.
        /// </summary>
        /// <param name="uimfFileLocation">
        /// The uimf file location.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <param name="netAlignment">
        /// The net alignment.
        /// </param>
        public LCMSPeptideSearchWorkfow(string uimfFileLocation, InformedParameters parameters, IInterpolation netAlignment) : this(uimfFileLocation, parameters)
        {
            this._netAlignment = netAlignment;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LCMSPeptideSearchWorkfow"/> class.
        /// </summary>
        /// <param name="uimfFileLocation">
        /// The uimf file location.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public LCMSPeptideSearchWorkfow(string uimfFileLocation, InformedParameters parameters)
        {
            this._buildWatershedStopWatch = new Stopwatch();
            this._smoothStopwatch = new Stopwatch();
            this._featureFindStopWatch = new Stopwatch();
            this._featureFindCount = 0;
            this._pointCount = 0;
            this._uimfFileLocation = uimfFileLocation;

            this._uimfReader = new DataReader(uimfFileLocation);

            // Append bin-centric table to the uimf if not present.
            if (!this._uimfReader.DoesContainBinCentricData())
            {
                DataWriter dataWriter = new DataWriter(uimfFileLocation);
                dataWriter.CreateBinCentricTables();
            }
            
            this._parameters = parameters;
            this._smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 2);
            this._theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();
            this._leftOfMonoPeakLooker = new LeftOfMonoPeakLooker();
            this._peakDetector = new ChromPeakDetector(0.0001, 0.0001);
            this._isotopicPeakFitScoreCalculator = new PeakLeastSquaresFitter();

            IterativeTFFParameters msFeatureFinderParameters = new IterativeTFFParameters
            {
                MinimumRelIntensityForForPeakInclusion = 0.0001,
                PeakDetectorMinimumPeakBR = 0.0001,
                PeakDetectorPeakBR = 5.0002,
                PeakBRStep = 0.25,
                PeakDetectorSigNoiseRatioThreshold = 0.0001,
                ToleranceInPPM = parameters.MassToleranceInPpm
            };
            this._msFeatureFinder = new IterativeTFF(msFeatureFinderParameters);
            this.NumberOfFrames = this._uimfReader.GetGlobalParams().NumFrames;
            this.NumberOfScans = this._uimfReader.GetFrameParams(1).Scans;
        }

        /// <summary>
        /// The run informed workflow.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="ChargeStateCorrelationResult"/>.
        /// </returns>
        public ChargeStateCorrelationResult RunInformedWorkflow(ImsTarget target)
        {
            Composition targetComposition = target.Composition;
            double targetMass = targetComposition.Mass;
            string empiricalFormula = targetComposition.ToPlainString();

            double targetNet = target.NormalizedElutionTime;
            double targetNetMin = targetNet - this._parameters.NetTolerance;
            double targetNetMax = targetNet + this._parameters.NetTolerance;

            double reverseAlignedNetMin = targetNetMin;
            double reverseAlignedNetMax = targetNetMax;

            if (this._netAlignment != null)
            {
                double reverseAlignedNet = this.GetReverseAlignedNet(targetNet);
                reverseAlignedNetMin = reverseAlignedNet - this._parameters.NetTolerance;
                reverseAlignedNetMax = reverseAlignedNet + this._parameters.NetTolerance;
            }

            int scanLcSearchMin = (int)Math.Floor(reverseAlignedNetMin * this.NumberOfFrames);
            int scanLcSearchMax = (int)Math.Ceiling(reverseAlignedNetMax * this.NumberOfFrames);

            int iteration = (targetComposition == null) ? 1 : this._parameters.ChargeStateMax;
            for (int chargeState = 1; chargeState <= iteration; chargeState++)
            {
                if (targetComposition != null) 
                {
                    Ion targetIon = new Ion(targetComposition, chargeState);
                    target.TargetMz = targetIon.GetMonoIsotopicMz();
                } 

                double minMzForSpectrum = target.TargetMz - (1.6 / chargeState);
                double maxMzForSpectrum = target.TargetMz + (4.6 / chargeState);
                
                // Generate Theoretical Isotopic Profile
                IsotopicProfile theoreticalIsotopicProfile = this._theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, chargeState);
                List<Peak> theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();

                // Find XIC Features
                IEnumerable<FeatureBlob> featureBlobs = this.FindFeatures(target.TargetMz, scanLcSearchMin, scanLcSearchMax);

                // Filter away small XIC peaks
                featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, 0.25);

                if(!featureBlobs.Any())
                {
                    ImsTargetResult result = new ImsTargetResult
                    {
                        ChargeState = chargeState,
                        AnalysisStatus = AnalysisStatus.XicNotFound
                    };

                    target.ResultList.Add(result);
                }

                // Check each XIC Peak found
                foreach (var featureBlob in featureBlobs)
                {
                    // Setup result object
                    ImsTargetResult result = new ImsTargetResult
                    {
                        ChargeState = chargeState,
                        AnalysisStatus = AnalysisStatus.Positive
                    };

                    target.ResultList.Add(result);

                    FeatureBlobStatistics statistics = featureBlob.CalculateStatistics();
                    int unsaturatedIsotope = 0;
                    FeatureBlob isotopeFeature = null;

                    int scanLcMin = statistics.ScanLcMin;
                    int scanLcMax = statistics.ScanLcMax;
                    int scanImsMin = statistics.ScanImsMin;
                    int scanImsMax = statistics.ScanImsMax;

                    // TODO: Verify that there are no peaks at isotope #s 0.5 and 1.5?? (If we filter on drift time, this shouldn't actually be necessary)

                    // Find an unsaturated peak in the isotopic profile
                    for (int i = 1; i < 10; i++)
                    {
                        if (!statistics.IsSaturated) break;

                        // Target isotope m/z 
                        double isotopeTargetMz = (target.Composition != null) ? new Ion(targetComposition, chargeState).GetIsotopeMz(i) : target.TargetMz;

                        // Find XIC Features
                        IEnumerable<FeatureBlob> newFeatureBlobs = this.FindFeatures(isotopeTargetMz, scanLcMin - 20, scanLcMax + 20);

                        // If no feature, then get out
                        if (!newFeatureBlobs.Any())
                        {
                            statistics = null;
                            break;
                        }

                        bool foundFeature = false;
                        foreach (var newFeatureBlob in newFeatureBlobs.OrderByDescending(x => x.PointList.Count))
                        {
                            var newStatistics = newFeatureBlob.CalculateStatistics();
                            if(newStatistics.ScanImsRep <= scanImsMax && newStatistics.ScanImsRep >= scanImsMin && newStatistics.ScanLcRep <= scanLcMax && newStatistics.ScanLcRep >= scanLcMin)
                            {
                                isotopeFeature = newFeatureBlob;
                                foundFeature = true;
                                break;
                            }
                        }

                        if(!foundFeature)
                        {
                            statistics = null;
                            break;
                        }

                        statistics = isotopeFeature.CalculateStatistics();
                        unsaturatedIsotope = i;
                    }

                    // Bad Feature, so get out
                    if (statistics == null)
                    {
                        result.AnalysisStatus = AnalysisStatus.IsotopicProfileNotFound;
                        continue;
                    }

                    // TODO: Calculate accurate NET and drift time using quadratic equation
                    int scanLcRep = statistics.ScanLcRep + 1;
                    int scanImsRep = statistics.ScanImsRep;

                    // Calculate NET using aligned data if applicable
                    double net = scanLcRep / this.NumberOfFrames;
                    if (this._netAlignment != null)
                    {
                        net = this._netAlignment.Interpolate(net);
                    }

                    FeatureBlob featureToUseForResult = unsaturatedIsotope > 0 ? isotopeFeature : featureBlob;

                    // Set data to result
                    result.FeatureBlobStatistics = statistics;
                    result.IsSaturated = unsaturatedIsotope > 0;
                    result.ScanLcRep = statistics.ScanLcRep;
                    result.NormalizedElutionTime = net;
                    result.DriftTime = this._uimfReader.GetDriftTime(statistics.ScanLcRep, statistics.ScanImsRep);
                    result.XicFeature = featureToUseForResult;

                    // Don't consider bogus results
                    if (scanImsRep < 5 || scanImsRep > this.NumberOfScans - 5)
                    {
                        result.AnalysisStatus = AnalysisStatus.DriftTimeError;
                        continue;
                    }


                        // Don't consider bogus results
                        if (scanLcRep < 3 || scanLcRep > this.NumberOfFrames - 4)
                        {
                            result.AnalysisStatus = AnalysisStatus.ElutionTimeError;
                            continue;
                        }

                        // TODO: Mass Alignment???
                    if (target.TargetType == TargetType.Peptide)
                    {
                        // Filter by NET
                        if (net > targetNetMax || net < targetNetMin)
                        {
                            result.AnalysisStatus = AnalysisStatus.ElutionTimeError;
                            continue;
                        }
                    }

                    //Console.WriteLine(target.PeptideSequence + "\t" + targetMass + "\t" + targetMz + "\t" + scanLcRep);

                    // Get Mass Spectrum Data
                    XYData massSpectrum = this.GetMassSpectrum(scanLcRep, scanImsRep, minMzForSpectrum, maxMzForSpectrum);
                    List<Peak> massSpectrumPeakList = this._peakDetector.FindPeaks(massSpectrum);
                    //WriteXYDataToFile(massSpectrum, targetMz);

                    // Find Isotopic Profile
                    List<Peak> massSpectrumPeaks;
                    IsotopicProfile observedIsotopicProfile = this._msFeatureFinder.IterativelyFindMSFeature(massSpectrum, theoreticalIsotopicProfile, out massSpectrumPeaks);

                    // Add data to result
                    result.MassSpectrum = massSpectrum;
                    
                    // No need to move on if the isotopic profile is not found
                    if (observedIsotopicProfile == null || observedIsotopicProfile.MonoIsotopicMass < 1)
                    {
                        result.AnalysisStatus = AnalysisStatus.IsotopicProfileNotFound;
                        continue;
                    }

                    // Add data to result
                    result.IsotopicProfile = observedIsotopicProfile;
                    result.MonoisotopicMass = observedIsotopicProfile.MonoIsotopicMass;
                    result.PpmError = Math.Abs(PeptideUtil.PpmError(targetMass, observedIsotopicProfile.MonoIsotopicMass));

                    // If not enough peaks to reach unsaturated isotope, no need to move on
                    if (observedIsotopicProfile.Peaklist.Count <= unsaturatedIsotope)
                    {
                        result.AnalysisStatus = AnalysisStatus.IsotopicProfileNotFound;
                        continue;
                    }

                    // If the mass error is too high, then ignore
                    if (result.PpmError > this._parameters.MassToleranceInPpm)
                    {
                        result.AnalysisStatus = AnalysisStatus.MassError;
                        continue;
                    }

                    // Correct for Saturation if needed
                    if (unsaturatedIsotope > 0)
                    {
                        IsotopicProfileUtil.AdjustSaturatedIsotopicProfile(observedIsotopicProfile, theoreticalIsotopicProfile, unsaturatedIsotope);
                    }

                    //WriteMSPeakListToFile(observedIsotopicProfile.Peaklist, targetMz);

                    // TODO: This is a hack to fix an issue where the peak width is being calculated way too large which causes the leftOfMonoPeakLooker to use too wide of a tolerance
                    MSPeak monoPeak = observedIsotopicProfile.getMonoPeak();
                    if (monoPeak.Width > 0.15) monoPeak.Width = 0.15f;

                    // Filter out flagged results
                    MSPeak peakToLeft = this._leftOfMonoPeakLooker.LookforPeakToTheLeftOfMonoPeak(monoPeak, observedIsotopicProfile.ChargeState, massSpectrumPeaks);
                    if (peakToLeft != null)
                    {
                        result.AnalysisStatus = AnalysisStatus.PeakToLeft;
                        continue;
                    }

                    double isotopicFitScore;

                    // Calculate isotopic fit score
                    if(unsaturatedIsotope > 0)
                    {
                        int unsaturatedScanLc = this.FindFrameNumberUseForIsotopicProfile(target.TargetMz, scanLcRep, scanImsRep);

                        if (unsaturatedScanLc > 0)
                        {
                            // Use the unsaturated profile if we were able to get one
                            XYData unsaturatedMassSpectrum = this.GetMassSpectrum(unsaturatedScanLc, scanImsRep, minMzForSpectrum, maxMzForSpectrum);
                            //WriteXYDataToFile(unsaturatedMassSpectrum, targetMz);
                            List<Peak> unsaturatedMassSpectrumPeakList = this._peakDetector.FindPeaks(unsaturatedMassSpectrum);
                            isotopicFitScore = this._isotopicPeakFitScoreCalculator.GetFit(theoreticalIsotopicProfilePeakList, unsaturatedMassSpectrumPeakList, 0.15, this._parameters.MassToleranceInPpm);
                        }
                        else
                        {
                            // Use the saturated profile
                            isotopicFitScore = this._isotopicPeakFitScoreCalculator.GetFit(theoreticalIsotopicProfilePeakList, massSpectrumPeakList, 0.15, this._parameters.MassToleranceInPpm);
                        }
                    }
                    else
                    {
                        isotopicFitScore = this._isotopicPeakFitScoreCalculator.GetFit(theoreticalIsotopicProfilePeakList, massSpectrumPeakList, 0.15, this._parameters.MassToleranceInPpm);
                    }

                    // Add data to result
                    result.IsotopicFitScore = isotopicFitScore;

                    // Filter out bad isotopic fit scores
                    if (isotopicFitScore > this._parameters.IsotopicFitScoreThreshold && unsaturatedIsotope == 0)
                    {
                        result.AnalysisStatus = AnalysisStatus.IsotopicFitScoreError;
                        continue;
                    }

                    Console.WriteLine(chargeState + "\t" + unsaturatedIsotope + "\t" + statistics.ScanLcMin + "\t" + statistics.ScanLcMax + "\t" + statistics.ScanLcRep + "\t" + statistics.ScanImsMin + "\t" + statistics.ScanImsMax + "\t" + statistics.ScanImsRep + "\t" + isotopicFitScore.ToString("0.0000") + "\t" + result.NormalizedElutionTime.ToString("0.0000") + "\t" + result.DriftTime.ToString("0.0000"));
                }

                // TODO: Isotope Correlation (probably not going to do because of saturation issues)
            }

            // Charge State Correlation (use first unsaturated XIC feature)
            List<ChargeStateCorrelationResult> chargeStateCorrelationResultList = new List<ChargeStateCorrelationResult>();
            ChargeStateCorrelationResult bestCorrelationResult = null;
            double bestCorrelationSum = -1;

            List<ImsTargetResult> resultList = target.ResultList.Where(x => x.AnalysisStatus == AnalysisStatus.Positive).OrderBy(x => x.IsotopicFitScore).ToList();
            int numResults = resultList.Count;

            for (int i = 0; i < numResults; i++)
            {
                ImsTargetResult referenceResult = resultList[i];

                ChargeStateCorrelationResult chargeStateCorrelationResult = new ChargeStateCorrelationResult(target, referenceResult);
                chargeStateCorrelationResultList.Add(chargeStateCorrelationResult);

                for (int j = i + 1; j < numResults; j++)
                {
                    ImsTargetResult testResult = resultList[j];
                    double correlation = FeatureCorrelator.CorrelateFeaturesUsingLc(referenceResult.XicFeature, testResult.XicFeature);
                    chargeStateCorrelationResult.CorrelationMap.Add(testResult, correlation);
                    Console.WriteLine(referenceResult.FeatureBlobStatistics.ScanLcRep + "\t" + referenceResult.FeatureBlobStatistics.ScanImsRep + "\t" + testResult.FeatureBlobStatistics.ScanLcRep + "\t" + testResult.FeatureBlobStatistics.ScanImsRep + "\t" + correlation);
                }

                List<ImsTargetResult> possibleBestResultList;
                double correlationSum = chargeStateCorrelationResult.GetBestCorrelation(out possibleBestResultList);

                if(correlationSum > bestCorrelationSum)
                {
                    bestCorrelationSum = correlationSum;
                    bestCorrelationResult = chargeStateCorrelationResult;
                }
            }

            // TODO: Score Target

            // TODO: Quantify Target (return isotopic profile abundance)

            return bestCorrelationResult;
        }

        public void ExtractData(IEnumerable<ImsTarget> targetList)
        {
            Stopwatch fastWatch = new Stopwatch();
            Stopwatch slowWatch = new Stopwatch();

            double totalChargeStateTargets = 0;

            foreach (var target in targetList)
            {
                // Get empirical formula
                Composition targetComposition = target.Composition;

                double targetNet = target.NormalizedElutionTime;
                double targetNetMin = targetNet - this._parameters.NetTolerance;
                double targetNetMax = targetNet + this._parameters.NetTolerance;

                double reverseAlignedNetMin = targetNetMin;
                double reverseAlignedNetMax = targetNetMax;

                if (this._netAlignment != null)
                {
                    double reverseAlignedNet = this.GetReverseAlignedNet(targetNet);
                    reverseAlignedNetMin = reverseAlignedNet - this._parameters.NetTolerance;
                    reverseAlignedNetMax = reverseAlignedNet + this._parameters.NetTolerance;
                }

                int scanLcSearchMin = (int)Math.Floor(reverseAlignedNetMin * this.NumberOfFrames);
                int scanLcSearchMax = (int)Math.Ceiling(reverseAlignedNetMax * this.NumberOfFrames);

                for (int chargeState = 1; chargeState <= this._parameters.ChargeStateMax; chargeState++)
                {
                    // Calculate Target m/z
                    var targetIon = new Ion(targetComposition, chargeState);
                    double targetMz = targetIon.GetMonoIsotopicMz();

                    if (targetMz > 2500) continue;

                    // Generate Chromatogram Fast
                    fastWatch.Start();
                    List<IntensityPoint> intensityPointList = this._uimfReader.GetXic(targetMz, this._parameters.MassToleranceInPpm, scanLcSearchMin, scanLcSearchMax, 0, 359, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
                    fastWatch.Stop();

                    // Generate Chromatogram Slow
                    slowWatch.Start();
                    FrameParams frameParameters = this._uimfReader.GetFrameParams(1);
                    double slope = frameParameters.CalibrationSlope;
                    double intercept = frameParameters.CalibrationIntercept;
                    double binWidth = this._uimfReader.GetGlobalParams().BinWidth;
                    float tofCorrectionTime = this._uimfReader.GetGlobalParams().TOFCorrectionTime;

                    double mzTolerance = targetMz / 1000000 * this._parameters.MassToleranceInPpm;
                    double lowMz = targetMz - mzTolerance;
                    double highMz = targetMz + mzTolerance;

                    int startBin = (int)Math.Floor(DataReader.GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, lowMz)) - 1;
                    int endBin = (int)Math.Ceiling(DataReader.GetBinClosestToMZ(slope, intercept, binWidth, tofCorrectionTime, highMz)) + 1;

                    int[][][] frameIntensities = this._uimfReader.GetIntensityBlock(scanLcSearchMin, scanLcSearchMax, DataReader.FrameType.MS1, 0, 359, startBin, endBin);
                    slowWatch.Stop();

                    totalChargeStateTargets++;
                }
            }

            double fastTimePerTarget = fastWatch.ElapsedMilliseconds / totalChargeStateTargets;
            double slowTimePerTarget = slowWatch.ElapsedMilliseconds / totalChargeStateTargets;

            Console.WriteLine("Num Targets = " + targetList.Count());
            Console.WriteLine("Num CS Targets = " + totalChargeStateTargets);
            Console.WriteLine("Fast = " + fastTimePerTarget + " ms per target.");
            Console.WriteLine("Slow = " + slowTimePerTarget + " ms per target.");
        }

        // Find the target Mz across different frames.
        protected IEnumerable<FeatureBlob> FindFeatures(double targetMz)
        {
            // Generate Chromatogram
            List<IntensityPoint> intensityPointList = this._uimfReader.GetXic(targetMz, this._parameters.MassToleranceInPpm, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

            // Smooth Chromatogram
            IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
            this._smoother.Smooth(ref pointList);

            // Peak Find Chromatogram
            IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);

            return featureBlobs;
        }

        // Find the target Mz across a range of frames.
        public IEnumerable<FeatureBlob> FindFeatures(double targetMz, int scanLcMin, int scanLcMax)
        {
            // Generate Chromatogram
            List<IntensityPoint> intensityPointList = this._uimfReader.GetXic(targetMz, this._parameters.MassToleranceInPpm, scanLcMin, scanLcMax, 0, 360, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

            if (intensityPointList == null || intensityPointList.Count == 0)
            {
                return new List<FeatureBlob>();
            }
        
            //WritePointsToFile(intensityPointList, targetMz);	

            // Smooth Chromatogram
            this._buildWatershedStopWatch.Start();
            IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
            this._buildWatershedStopWatch.Stop();

            this._smoothStopwatch.Start();
            this._smoother.Smooth(ref pointList);
            this._smoothStopwatch.Stop();

            // Peak Find Chromatogram
            this._featureFindStopWatch.Start();
            IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);
            this._featureFindStopWatch.Stop();

            this._featureFindCount++;
            this._pointCount += pointList.Count();

            return featureBlobs;
        }

        public void PrintFeatureFindStatistics()
        {
            double buildWatershedTime = this._buildWatershedStopWatch.ElapsedMilliseconds / this._featureFindCount;
            double smoothTime = this._smoothStopwatch.ElapsedMilliseconds / this._featureFindCount;
            double featureFindTime = this._featureFindStopWatch.ElapsedMilliseconds / this._featureFindCount;

            Console.WriteLine("Num XICs Searched = " + this._featureFindCount);
            Console.WriteLine("Num Points Searched = " + this._pointCount);
            Console.WriteLine("Build Watershed = " + buildWatershedTime + " ms per XIC.");
            Console.WriteLine("Smooth = " + smoothTime + " ms per XIC.");
            Console.WriteLine("Feature Find = " + featureFindTime + " ms per XIC.\r\n");
        }

        protected void WritePointsToFile(IEnumerable<IntensityPoint> intensityPointList, double targetMz)
        {
            intensityPointList = intensityPointList.OrderBy(x => x.ScanLc).ThenBy(x => x.ScanIms);
            
            int minScanLc = intensityPointList.First().ScanLc;

            StringBuilder stringBuilder = new StringBuilder();

            using (StreamWriter writer = new StreamWriter("points" + Math.Round(targetMz, 3) + ".csv"))
            {
                int currentScanLc = minScanLc;
                int currentScanIms = 0;

                stringBuilder.Append(",");
                for (int i = 0; i < 360; i++)
                {
                    stringBuilder.Append(i + ",");
                }

                stringBuilder.Append("\n" + currentScanLc + ",");

                foreach (var intensityPoint in intensityPointList)
                {
                    int pointScanLc = intensityPoint.ScanLc;
                    int pointScanIms = intensityPoint.ScanIms;
                    double intensity = intensityPoint.Intensity;

                    while (pointScanLc > currentScanLc)
                    {
                        while (currentScanIms < 360)
                        {
                            stringBuilder.Append("0,");
                            currentScanIms++;
                        }

                        stringBuilder.Append("\n");
                        currentScanIms = 0;
                        currentScanLc++;

                        stringBuilder.Append(currentScanLc + ",");
                    }

                    while (pointScanIms > currentScanIms)
                    {
                        stringBuilder.Append("0,");
                        currentScanIms++;
                    }

                    stringBuilder.Append(intensity + ",");
                    currentScanIms++;
                }

                while (currentScanIms < 360)
                {
                    stringBuilder.Append("0,");
                    currentScanIms++;
                }

                stringBuilder.Append("\n");

                writer.Write(stringBuilder.ToString());
            }
        }

        protected void WriteXYDataToFile(XYData xyData, double targetMz)
        {
            double[] xValues = xyData.Xvalues;
            double[] yValues = xyData.Yvalues;

            using (StreamWriter writer = new StreamWriter("xydata" + Math.Round(targetMz, 3) + ".csv"))
            {
                for(int i = 0; i < xValues.Length; i++)
                {
                    writer.WriteLine(Math.Round(xValues[i], 3) + "," + yValues[i]);
                }
            }
        }

        protected void WriteMSPeakListToFile(IEnumerable<MSPeak> peakList, double targetMz)
        {
            using (StreamWriter writer = new StreamWriter("mspeaks" + Math.Round(targetMz, 3) + ".csv"))
            {
                foreach (var msPeak in peakList)
                {
                    writer.WriteLine(Math.Round(msPeak.XValue, 3) + "," + msPeak.Height);
                }
            }
        }

        protected int FindFrameNumberUseForIsotopicProfile(double targetMz, int scanLcRep, int scanImsRep)
        {
            int returnScanLc = -1;
            int scanLcToTry = scanLcRep;

            while (returnScanLc < 0)
            {
                scanLcToTry--;

                // Quit looking if we get to the beginning of the spectrum
                if (scanLcToTry <= 0) break;

                // Generate Chromatogram
                List<IntensityPoint> intensityPointList = this._uimfReader.GetXic(targetMz, this._parameters.MassToleranceInPpm, scanLcToTry, scanLcToTry, scanImsRep, scanImsRep, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

                bool foundSaturated = intensityPointList.Any(intensityPoint => intensityPoint.IsSaturated);

                if(!foundSaturated) returnScanLc = scanLcToTry;
            }

            // This means searching to the left failed, so search to the right
            if(returnScanLc < 0)
            {
                scanLcToTry = scanLcRep;

                while (returnScanLc < 0)
                {
                    scanLcToTry++;

                    // Generate Chromatogram
                    List<IntensityPoint> intensityPointList = this._uimfReader.GetXic(targetMz, this._parameters.MassToleranceInPpm, scanLcToTry, scanLcToTry, scanImsRep, scanImsRep, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

                    bool foundSaturated = intensityPointList.Any(intensityPoint => intensityPoint.IsSaturated);

                    if (!foundSaturated) returnScanLc = scanLcToTry;
                }
            }

            return returnScanLc;
        }

        protected XYData GetMassSpectrum(int scanLcRep, int scanImsRep, double minMzForSpectrum, double maxMzForSpectrum)
        {
            double[] mzArray;
            int[] intensityArray;

            this._uimfReader.GetSpectrum(scanLcRep - 1, scanLcRep + 1, DataReader.FrameType.MS1, scanImsRep - 2, scanImsRep + 2, minMzForSpectrum, maxMzForSpectrum, out mzArray, out intensityArray);
            double[] intensityArrayAsDoubles = XYData.ConvertIntsToDouble(intensityArray);
            XYData massSpectrum = new XYData();
            massSpectrum.SetXYValues(ref mzArray, ref intensityArrayAsDoubles);

            return massSpectrum;
        }

        private double GetReverseAlignedNet(double net)
        {
            double difference = 2;

            for (double d = 0; d <= 1; d += 0.01)
            {
                double alignedNet = this._netAlignment.Interpolate(d);
                double newDifference = Math.Abs(net - alignedNet);

                if (newDifference > difference) return d;

                difference = newDifference;
            }

            return 0;
        }
    }
}
