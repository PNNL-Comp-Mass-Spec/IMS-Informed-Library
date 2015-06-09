// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrossSectionWorkfow.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using DeconTools.Backend.Core;
    using DeconTools.Backend.ProcessingTasks.PeakDetectors;
    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DataAssociation;
    using ImsInformed.Domain.DataAssociation.IonTrackers;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Filters;
    using ImsInformed.IO;
    using ImsInformed.Scoring;
    using ImsInformed.Targets;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Composition;

    using MagnitudeConcavityPeakFinder;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    /// <summary>
    /// Find molecules with a given empirical formula and given ionization methods. Developed based on LCMS Peptide Search Workflow
    /// </summary>
    public class CrossSectionWorkfow : IDisposable
    {
        /// <summary>
        /// The number of frames.
        /// </summary>
        public readonly int NumberOfFrames;

        /// <summary>
        /// The number of scans.
        /// </summary>
        public readonly int NumberOfScans;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionWorkfow"/> class.
        /// </summary>
        /// <param name="uimfFileLocation">
        /// The uimf file location.
        /// </param>
        /// <param name="outputDirectory">
        /// The output directory.
        /// </param>
        /// <param name="logFileName">
        /// The log file name.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public CrossSectionWorkfow(string uimfFileLocation, string outputDirectory, CrossSectionSearchParameters parameters)
        {
            this.uimfReader = new DataReader(uimfFileLocation);

            // Append bin-centric table to the uimf if not present.
            if (!this.uimfReader.DoesContainBinCentricData())
            {
                DataWriter dataWriter = new DataWriter(uimfFileLocation);
                dataWriter.CreateBinCentricTables();
            }
            
            this.Parameters = parameters;
            this.smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 2);
            this.theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();
            this.peakDetector = new ChromPeakDetector(0.0001, 0.0001);

            this.NumberOfFrames = this.uimfReader.GetGlobalParams().NumFrames;
            this.NumberOfScans = this.uimfReader.GetFrameParams(1).Scans;

            this.DatasetName = Path.GetFileNameWithoutExtension(uimfFileLocation);

            this.Parameters = parameters;

            if (outputDirectory == string.Empty)
            {
                outputDirectory = Directory.GetCurrentDirectory();
            } 
            
            if (!outputDirectory.EndsWith("\\"))
            {
                outputDirectory += "\\";
            }

            if (!Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to create directory.");
                    throw;
                }
            }

            this.OutputPath = outputDirectory;
        }

        /// <summary>
        /// The UIMF reader.
        /// </summary>
        public readonly DataReader uimfReader;

        /// <summary>
        /// The theoretical feature generator.
        /// </summary>
        protected readonly ITheorFeatureGenerator theoreticalFeatureGenerator;

        /// <summary>
        /// The smoother.
        /// </summary>
        protected readonly SavitzkyGolaySmoother smoother;

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public CrossSectionSearchParameters Parameters { get; set; }

        /// <summary>
        /// Gets or sets the dataset name.
        /// </summary>
        public string DatasetName { get; set; }

         /// <summary>
        /// The _peak detector.
        /// </summary>
        protected readonly ChromPeakDetector peakDetector;

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// The run cross section work flow.
        /// </summary>
        /// <param name="targetList">
        /// The target list.
        /// </param>
        /// <param name="detailedVerbose">
        /// The detailed verbose.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public IList<CrossSectionWorkflowResult> RunCrossSectionWorkFlow(IEnumerable<IImsTarget> targetList, bool detailedVerbose = true)
        {
            IList<CrossSectionWorkflowResult> targetResultMap = new List<CrossSectionWorkflowResult>();
            foreach (IImsTarget target in targetList)
            {
                if (target.EmpiricalFormula != null)
                {
                    Console.Write("    Target: " + target.EmpiricalFormula);
                    Console.WriteLine(" (MZ = {0})", target.MonoisotopicMass);
                }
                else
                {
                    Console.WriteLine("    Target: Unknown (MZ = {0})", target.MassWithAdduct);
                }

                CrossSectionWorkflowResult result = this.RunCrossSectionWorkFlow(target, detailedVerbose);
                targetResultMap.Add(result);
            }

            return targetResultMap;
        }

        /// <summary>
        /// The run molecule informed work flow.
        /// </summary>
        /// <param name="target">
        /// The Target.
        /// </param>
        /// <param name="detailedVerbose">
        /// </param>
        /// <returns>
        /// The <see cref="CrossSectionWorkflowResult"/>.
        /// </returns>
        public CrossSectionWorkflowResult RunCrossSectionWorkFlow(IImsTarget target, bool detailedVerbose = true)
        {
            // Reassign trace listener to print to console as well as the target directory.
            using (this.ResetTraceListenerToTarget(target))
            {
                try
                {
                    // Get the monoisotopic mass for viper, which is different from anything else.
                    double viperFriendlyMass = target.MassWithAdduct;
                    if (target.ChargeState < 0)
                    {
                        viperFriendlyMass = viperFriendlyMass + new Composition(0, target.ChargeState, 0, 0, 0).Mass;
                    }
                    else
                    {
                        viperFriendlyMass = viperFriendlyMass - new Composition(0, Math.Abs(target.ChargeState), 0, 0, 0).Mass;
                    }

                    // Generate Theoretical Isotopic Profile
                    List<Peak> theoreticalIsotopicProfilePeakList = null;
                    if (target.HasCompositionInfo) 
                    {
                        int chargeStateAbs = Math.Abs(target.ChargeState);
                        Composition compensatedComposition = target.CompositionWithAdduct - new Composition(0, chargeStateAbs, 0, 0, 0);
                        string empiricalFormula = compensatedComposition.ToPlainString();
                        
                        // Again this isotopic profile generator auto adds hydrogen for you depending on charge states. So here take it out.
                        IsotopicProfile theoreticalIsotopicProfile = this.theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, chargeStateAbs);

                        theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
                    }

                    ReportTargetInfo(target, detailedVerbose);
                    
                    double targetMz = Math.Abs(target.MassWithAdduct / target.ChargeState);

                    // Voltage grouping. Note that we only accumulate frames as needed. Accumulate frames globally is too costly. 
                    // Here we accumulate the XICs around target MZ.
                    VoltageSeparatedAccumulatedXiCs accumulatedXiCs = new VoltageSeparatedAccumulatedXiCs(this.uimfReader, targetMz, this.Parameters.MassToleranceInPpm);

                    // Perform feature detection and scoring and the given Mz range on the accumulated XICs to get the base peaks.
                    if (detailedVerbose)
                    {
                        Trace.WriteLine("Feature detection and scoring: ");
                    }

                    IList<VoltageGroup> rejectedVoltageGroups = new List<VoltageGroup>();
                    IList<ObservedPeak> filteredObservations = new List<ObservedPeak>();
                    IList<ObservedPeak> allObservations = new List<ObservedPeak>();
                    IList<ObservedPeak> rejectedObservations = new List<ObservedPeak>();

                    // Iterate through the features and perform filtering on isotopic affinity, intensity, drift time and peak shape.
                    foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                    {    
                        double globalMaxIntensity = IMSUtil.MaxIntensityAfterFrameAccumulation(voltageGroup, this.uimfReader);
                    
                        List<StandardImsPeak> standardPeaks = this.FindPeaksBasedOnXIC(voltageGroup, accumulatedXiCs[voltageGroup], target);
                        
                        // Score features
                        IDictionary<StandardImsPeak, FeatureStatistics> scoresTable = new Dictionary<StandardImsPeak, FeatureStatistics>();
                        if (detailedVerbose)
                        {
                            Trace.WriteLine(
                                string.Format(
                                    "    Voltage Group: {0:F4} V, [{1}-{2}]",
                                    voltageGroup.MeanVoltageInVolts,
                                    voltageGroup.FirstFirstFrameNumber, 
                                voltageGroup.LastFrameNumber));
                        }

                        foreach (StandardImsPeak peak in standardPeaks)
                        {   
                            FeatureStatistics currentStatistics = FeatureScoreUtilities.ScoreFeature(
                                peak, 
                                globalMaxIntensity,
                                this.uimfReader,
                                this.Parameters.MassToleranceInPpm,
                                this.Parameters.DriftTimeToleranceInMs,
                                voltageGroup,
                                this.NumberOfScans,
                                target,
                                IsotopicScoreMethod.Angle, 
                                theoreticalIsotopicProfilePeakList);
                    
                            scoresTable.Add(peak, currentStatistics);
                        }
                    
                        // 2st round filtering: filter out non Target peaks and noise. 
                        Predicate<StandardImsPeak> intensityThreshold = imsPeak => FeatureFilters.FilterLowIntensity(imsPeak, scoresTable[imsPeak].IntensityScore, this.Parameters.IntensityThreshold);
                    
                        // filter out features with Ims scans at 1% left or right.
                        Predicate<StandardImsPeak> scanPredicate = imsPeak => FeatureFilters.FilterExtremeDriftTime(imsPeak, this.NumberOfScans);

                        // filter out features with bad peak shapes.
                        Predicate<StandardImsPeak> shapeThreshold = imsPeak => FeatureFilters.FilterBadPeakShape(imsPeak, scoresTable[imsPeak].PeakShapeScore, this.Parameters.PeakShapeThreshold);

                        // filter out features with distant isotopic profile.
                        Predicate<StandardImsPeak> isotopeThreshold = imsPeak => FeatureFilters.FilterBadIsotopicProfile(imsPeak, scoresTable[imsPeak].IsotopicScore, this.Parameters.IsotopicThreshold);
                    
                        // Print out candidate features and how they were rejected.
                        foreach (StandardImsPeak peak in standardPeaks)
                        {
                            FeatureStatistics currentStatistics = scoresTable[peak];
                            bool pass = ReportFeatureEvaluation(
                                peak,
                                currentStatistics,
                                detailedVerbose, 
                                target, 
                                scanPredicate(peak),
                                intensityThreshold(peak),
                                shapeThreshold(peak),
                                intensityThreshold(peak));   
                         
                            ObservedPeak analyzedPeak = new ObservedPeak(voltageGroup, peak, currentStatistics);
                            if (pass)
                            {
                                filteredObservations.Add(analyzedPeak);
                            }
                            else
                            {
                                rejectedObservations.Add(analyzedPeak);
                            }

                            allObservations.Add(analyzedPeak);
                        }
                        
                        standardPeaks.RemoveAll(scanPredicate);
                    
                        standardPeaks.RemoveAll(intensityThreshold);
                    
                        standardPeaks.RemoveAll(shapeThreshold);
                        
                        if (target.HasCompositionInfo)
                        {
                            standardPeaks.RemoveAll(isotopeThreshold);
                        }

                        if (standardPeaks.Count == 0)
                        {
                            if (detailedVerbose)
                            {
                                Trace.WriteLine(string.Format("    (All features were rejected in voltage group {0:F4} V)",     voltageGroup.MeanVoltageInVolts));
                                Trace.WriteLine(string.Empty);
                                Trace.WriteLine(string.Empty);
                            }
                    
                            rejectedVoltageGroups.Add(voltageGroup);
                        }
                    
                        // Rate the feature's VoltageGroupScoring score. VoltageGroupScoring score measures how likely the voltage group contains and detected the Target ion.
                        voltageGroup.VoltageGroupScore = VoltageGroupScoring.ComputeVoltageGroupStabilityScore(voltageGroup);
                    }
                    
                    // Remove voltage groups that were rejected
                    foreach (VoltageGroup voltageGroup in rejectedVoltageGroups)
                    {
                        accumulatedXiCs.Remove(voltageGroup);
                    }

                    // Report analysis as negative
                    if (accumulatedXiCs.Keys.Count == 0)
                    {
                        var informedResult = CrossSectionWorkflowResult.CreateNegativeResult(rejectedObservations, rejectedVoltageGroups, this.DatasetName, target);
                        ReportAnslysisResultAndMetrics(informedResult, detailedVerbose);
                        return informedResult;
                    }
                    else
                    {
                    // Perform the data association algorithm.
                    IIonTracker tracker = new CombinatorialIonTracker(1000);

                    // Because for somereason we are not keeping track of drift tube length in UIMF...so we kind of have to go ask the instrument operator..
                    double driftTubeLength = FakeUIMFReader.DriftTubeLengthInCentimeters;
                    AssociationHypothesis optimalAssociationHypothesis = tracker.FindOptimumHypothesis(filteredObservations, driftTubeLength, target, this.Parameters);

                    if (detailedVerbose)
                    {
                        Console.WriteLine("Writes QC plot of fitline to " + this.OutputPath);
                        Trace.WriteLine(string.Empty);
                    }

                    string outputPath = this.OutputPath + this.DatasetName + "_" + target.TargetDescriptor + "_QA.png";
                    ImsInformedPlotter.PlotAssociationHypothesis(optimalAssociationHypothesis, outputPath, this.DatasetName, target.TargetDescriptor);

                    // Printout results
                    if (detailedVerbose)
                    {
                        Trace.WriteLine("Target Data Association");
                    }
                   
                    // Remove outliers with high influence.
                    // FitLine.RemoveOutliersAboveThreshold(3, minFitPoints);
                    
                    // Remove outliers until min fit point is reached or good R2 is achieved.
                    // while (TrackFilter.IsLowR2(FitLine.RSquared) && FitLine.FitPointCollection.Count > minFitPoints)
                    // {
                    //     FitLine.RemoveOutlierWithHighestCookDistance(minFitPoints);
                    // }
                    
                    // Remove the voltage considered outliers
                    // foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys.Where(p => FitLine.OutlierCollection.Contains(p.FitPoint)).ToList())
                    // {
                    //     accumulatedXiCs.Remove(voltageGroup);
                    // }
                        
                    CrossSectionWorkflowResult informedResult = CrossSectionWorkflowResult.CreateResultFromAssociationHypothesis(
                        this.Parameters, 
                        this.DatasetName,
                        optimalAssociationHypothesis, 
                        target,
                        accumulatedXiCs.Keys,
                        allObservations,
                        viperFriendlyMass);
                        ReportAnslysisResultAndMetrics(informedResult, detailedVerbose);

                        Trace.Listeners.Clear();
                    return informedResult;
                    }
                }
                catch (Exception e)
                {
                    // Print result
                    Trace.WriteLine(e.Message);
                    Trace.WriteLine(e.StackTrace);
                    Trace.Listeners.Clear();
                    Trace.Close();
                        Trace.Listeners.Clear();

                    // create the error result
                    return CrossSectionWorkflowResult.CreateErrorResult(target, this.DatasetName);
                }
            }
        }

        /// <summary>
        /// The reset trace listener to target.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="FileStream"/>.
        /// </returns>
        private FileStream ResetTraceListenerToTarget(IImsTarget target)
        {
            Trace.Listeners.Clear();
            string targetResultFileName = Path.Combine(this.OutputPath, "TargetSearchResult" + target.TargetDescriptor + ".txt");
            FileStream resultFile = new FileStream(targetResultFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            ConsoleTraceListener consoleTraceListener = new ConsoleTraceListener(false);
            consoleTraceListener.TraceOutputOptions = TraceOptions.DateTime;
            TextWriterTraceListener targetResultTraceListener = new TextWriterTraceListener(resultFile)
            {
                TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
            };

            Trace.Listeners.Add(consoleTraceListener);
            Trace.Listeners.Add(targetResultTraceListener);
            Trace.AutoFlush = true;
            return resultFile;
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CrossSectionWorkfow"/> class. 
        /// Finalizer
        /// </summary>
        ~CrossSectionWorkfow()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        public virtual void Dispose(bool disposing)
        {
            if (disposing) 
            {
                // free managed resources
                this.uimfReader.Dispose();
                this.DatasetName = null;
                this.OutputPath = null;
            }

            // free native resources if there are any.
            Trace.Listeners.Clear();
        }

        /// <summary>
        /// The report anslysis result and metrics.
        /// </summary>
        /// <param name="informedResult">
        /// The informed Result.
        /// </param>
        /// <param name="detailedVerbose">
        /// The detailed verbose.
        /// </param>
        private static void ReportAnslysisResultAndMetrics(CrossSectionWorkflowResult informedResult, bool detailedVerbose = true)
        {
            Trace.WriteLine("Analysis result");
            Trace.WriteLine(string.Format("    Analysis Conclusion: {0}", informedResult.AnalysisStatus));
            if (informedResult.AnalysisStatus == AnalysisStatus.Positive)
            {
                Trace.WriteLine(string.Format("    P(T|X): {0:F4}", informedResult.AssociationHypothesisInfo.ProbabilityOfDataGivenHypothesis));
                Trace.WriteLine(string.Format("    P(X|T): {0:F4}", informedResult.AssociationHypothesisInfo.ProbabilityOfDataGivenHypothesis));
            }

            if (detailedVerbose)
            {
                Trace.WriteLine(string.Format("    Average Voltage Group Stability Scor{0:F4}", informedResult.AverageVoltageGroupStability));
                Trace.WriteLine(string.Format("    Average Candidate Target Intensity Score {0:F4}", informedResult.AverageObservedPeakStatistics.IntensityScore));
                
                if (informedResult.Target.HasCompositionInfo)
                {
                    Trace.WriteLine(string.Format("    Average Candidate Target Isotopic Score {0:F4}", informedResult.AverageObservedPeakStatistics.IsotopicScore));
                }

                Trace.WriteLine(
                    string.Format(
                        "    Average Candidate Target Peak Shape S{0:F4}", 
                        informedResult.AverageObservedPeakStatistics.PeakShapeScore));

                if (informedResult.AnalysisStatus == AnalysisStatus.Positive)
                {
                    ReportIsomersInfo(informedResult.IdentifiedIsomers);
                }
            }
        }

        /// <summary>
        /// The report feature evaluation.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <param name="scores">
        /// The scores.
        /// </param>
        /// <param name="verbose">
        /// The verbose.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="badScanRange">
        /// The bad scan range.
        /// </param>
        /// <param name="lowIntensity">
        /// The low intensity.
        /// </param>
        /// <param name="badPeakShape">
        /// The bad peak shape.
        /// </param>
        /// <param name="lowIsotopicAffinity">
        /// The low isotopic affinity.
        /// </param>
        /// <returns>
        /// If the feature pass all the filters <see cref="bool"/>.
        /// </returns>
        private static bool ReportFeatureEvaluation(StandardImsPeak peak, FeatureStatistics scores, bool verbose, IImsTarget target, bool badScanRange, bool lowIntensity, bool badPeakShape, bool lowIsotopicAffinity)
        {
            Trace.WriteLine(string.Format("        Candidate feature found at [centerMz = {0:F4}, drift time = {1:F2} ms    (scan# = {2})] ", peak.HighestPeakApex.MzCenterInDalton, peak.HighestPeakApex.DriftTimeCenterInMs,     peak.HighestPeakApex.DriftTimeCenterInScanNumber));
            Trace.WriteLine(string.Format("            IntensityScore: {0:F4}", scores.IntensityScore));
            
            if (!lowIntensity)
            {
                Trace.WriteLine(string.Format("            peakShapeScore: {0:F4}", scores.PeakShapeScore));
            
                if (target.HasCompositionInfo)
                {
                    Trace.WriteLine(string.Format("            isotopicScore:  {0:F4}", scores.IsotopicScore));
                }
            }

            string rejectionReason = badScanRange ? "        [Bad scan range] " : "        ";
            rejectionReason += lowIntensity ? "[Low Intensity] " : string.Empty;
            rejectionReason += !lowIntensity && badPeakShape ? "[Bad Peak Shape] " : string.Empty;
            rejectionReason += !lowIntensity && lowIsotopicAffinity ? "[Different Isotopic Profile] " : string.Empty;

            bool rejected = badScanRange || lowIntensity || lowIsotopicAffinity || badPeakShape;

            if (verbose)
            {
                if (rejected)
                {
                    Trace.WriteLine(rejectionReason);
                }
                else
                {
                    Trace.WriteLine("        [PASS]");
                }

                Trace.WriteLine(string.Empty);
            }

            return !rejected;
        }

        /// <summary>
        /// The report track information.
        /// </summary>
        /// <param name="hypothesis">
        /// The hypothesis.
        /// </param>
        /// <param name="hasCompositionInfo">
        /// The has Composition Info.
        /// </param>
        /// <param name="verbose">
        /// The verbose.
        /// </param>
        private static void ReportTrackInformation(AssociationHypothesis hypothesis, bool hasCompositionInfo, bool verbose)
        {
            foreach (IsomerTrack track in hypothesis.Tracks)
            {
                foreach (var observation in track.ObservedPeaks)
                {
                    VoltageGroup voltageGroup = observation.VoltageGroup;
                    StandardImsPeak peak = observation.Peak;
                    double driftTimeInMs = peak.HighestPeakApex.DriftTimeCenterInMs;
                    
                    // Normalize the drift time to be displayed.
                    double normalizedDriftTimeInMs = IMSUtil.NormalizeDriftTime(driftTimeInMs, voltageGroup);

                    if (verbose)
                    {
                        Trace.WriteLine(
                            string.Format(
                                "    Target presence confirmed at {0:F2} ± {1:F2} V.",
                                voltageGroup.MeanVoltageInVolts,
                                Math.Sqrt(voltageGroup.VarianceVoltage)));
                    
                        Trace.WriteLine(
                            string.Format(
                                "        Frame range: [{0}, {1}]",
                                voltageGroup.FirstFirstFrameNumber,
                                voltageGroup.LastFrameNumber));
                    
                        Trace.WriteLine(
                            string.Format(
                                "        Normalized Drift Time: {0:F4} ms (Scan# = {1})",
                                normalizedDriftTimeInMs,
                                peak.HighestPeakApex.DriftTimeCenterInScanNumber));

                        Trace.WriteLine(string.Format("        VoltageGroupScoring: {0:F4}", voltageGroup.VoltageGroupScore));
                        Trace.WriteLine(string.Format("        IntensityScore: {0:F4}", observation.Statistics.IntensityScore));
                        if (hasCompositionInfo)
                        {
                            Trace.WriteLine(string.Format("        IsotopicScore: {0:F4}", observation.Statistics.IsotopicScore));
                        }
            
                        Trace.WriteLine(string.Format("        PeakShapeScore: {0:F4}", observation.Statistics.PeakShapeScore));
                        Trace.WriteLine(string.Empty);

                    }
                }
            }
        }

        /// <summary>
        /// The find peaks based on xic.
        /// </summary>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <param name="chromatogram">
        /// The chromatogram.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="IList"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        private List<StandardImsPeak> FindPeaksBasedOnXIC(VoltageGroup voltageGroup, ExtractedIonChromatogram chromatogram, IImsTarget target)
        {
            if (this.Parameters.PeakDetectorSelection == PeakDetectorEnum.WaterShed)
            {
                // Find peaks using multidimensional peak finder.
                List<IntensityPoint> intensityPoints = chromatogram.IntensityPoints;
                List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, this.smoother, this.Parameters.FeatureFilterLevel);

                // Recapture the 2D peak using the 1D feature blob from multidimensional peak finder.
                return featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, this.uimfReader, voltageGroup, target.MassWithAdduct, this.Parameters.MassToleranceInPpm)).ToList();
            } 
            else if (this.Parameters.PeakDetectorSelection == PeakDetectorEnum.MASICPeakFinder)
            {
                // Find peaks using MASIC peak finder
                List<IntensityPoint> intensityPoints = chromatogram.IntensityPoints;
                IList<clsPeak> masicPeaks = PeakFinding.FindPeakUsingMasic(intensityPoints, this.NumberOfScans);
                
                // Recapture the 2D peak using the 1D feature blob from multidimensional peak finder.
                return masicPeaks.Select(peak => new StandardImsPeak(peak)).ToList();
            }
            else
            {
                throw new NotImplementedException(string.Format("{0} not supported", this.Parameters.PeakDetectorSelection));
            }
        }

        /// <summary>
        /// The report target info.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="verbose">
        /// The verbose.
        /// </param>
        private static void ReportTargetInfo(IImsTarget target, bool verbose)
        {
            Trace.WriteLine(string.Empty);

            if (verbose)
            {
                Trace.WriteLine("Target chemical: " + target.ChemicalIdentifier);
                Trace.WriteLine("Target description: " + target.TargetDescriptor);
                if (target.HasCompositionInfo)
                {
                    Trace.WriteLine("Target Empirical Formula: " + target.EmpiricalFormula);
                }

                Trace.WriteLine("Targeting centerMz: " + target.MassWithAdduct);
                Trace.WriteLine(string.Empty);
            } 
            else
            {
                Trace.WriteLine("Target: " + target.TargetDescriptor);
            }
        }

        /// <summary>
        /// The report isomers info.
        /// </summary>
        /// <param name="allIsomers">
        /// The all isomers.
        /// </param>
        private static void ReportIsomersInfo(IEnumerable<IdentifiedIsomerInfo> allIsomers)
        {
            int isomerIndex = 1;
            var isomers = allIsomers.ToArray();
            bool onlyOneIsomer = isomers.Count() <= 1;
            foreach (IdentifiedIsomerInfo isomer in isomers)
            {
                if (!onlyOneIsomer)
                {
                    Trace.WriteLine(string.Format("    Isomer #[{0}]", isomerIndex));
                }
            
                Trace.WriteLine(string.Format("    Mobility: {0:F4} cm^2/(s*V)", isomer.Mobility));
                Trace.WriteLine(string.Format("    Cross Sectional Area: {0:F4} Å^2", isomer.CrossSectionalArea));
                ArrivalTimeSnapShot lastDriftTime = isomer.ArrivalTimeSnapShots.Last();

                Trace.WriteLine(string.Format("    Last VoltageGroup Drift Time: {0:F4} ms [V: {1:F2}, T: {2:F2}K, P: {3:F2}]", lastDriftTime.MeasuredArrivalTimeInMs, lastDriftTime.DriftTubeVoltageInVolt, lastDriftTime.TemperatureInKelvin, lastDriftTime.PressureInTorr));
                
                isomerIndex++;
            
                if (!onlyOneIsomer)
                {
                    Trace.WriteLine(string.Empty);
                }
            }
        }
    }
}
