﻿// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
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
namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using System;
    using System.Collections;
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

            this.smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 5);

            this.theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();
            this.peakDetector = new ChromPeakDetector(0.0001, 0.0001);

            this.NumberOfFrames = this.uimfReader.GetGlobalParams().NumFrames;
            this.NumberOfScans = this.uimfReader.GetFrameParams(1).Scans;
            this.SampleCollectionDate = this.uimfReader.GetGlobalParams().GetValue(GlobalParamKeyType.DateStarted);

            this.DatasetName = Path.GetFileNameWithoutExtension(uimfFileLocation);
            this.OutputPath = outputDirectory;
            this.DatasetPath = uimfFileLocation;

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
        public CrossSectionSearchParameters Parameters { get; private set; }

        /// <summary>
        /// Gets or sets the dataset name.
        /// </summary>
        public string DatasetName { get; private set; }

        public string SampleCollectionDate { get; private set; }

        /// <summary>
        /// Gets or sets the dataset name.
        /// </summary>
        public string DatasetPath { get; private set; }

         /// <summary>
        /// The _peak detector.
        /// </summary>
        protected readonly ChromPeakDetector peakDetector;

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputPath { get; private set; }

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
            using (this.ResetTraceListenerToTarget(target, this.DatasetName))
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

                        // Again this isotopic profile generator auto adds hydrogen for you depending on charge states. So here take it out.
                        Composition compensatedComposition = target.CompositionWithAdduct - new Composition(0, chargeStateAbs, 0, 0, 0);
                        string empiricalFormula = compensatedComposition.ToPlainString();
                        
                        IsotopicProfile theoreticalIsotopicProfile = this.theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, chargeStateAbs);

                        theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
                    }

                    Trace.WriteLine(string.Format("Dataset: {0}", this.uimfReader.UimfFilePath));
                    ReportTargetInfo(target, detailedVerbose);
                    
                    
                    double targetMz = Math.Abs(target.MassWithAdduct / target.ChargeState);

                    // Voltage grouping. Note that we only accumulate frames as needed. Accumulate frames globally is too costly. 
                    // Here we accumulate the XICs around target MZ.
                    VoltageSeparatedAccumulatedXiCs accumulatedXiCs = new VoltageSeparatedAccumulatedXiCs(this.uimfReader, targetMz, this.Parameters.MzWindowHalfWidthInPpm, this.Parameters.DriftTubeLengthInCm);

                    // Remove voltage groups that don't have sufficient frame accumulations
                    IEnumerable<VoltageGroup> remainingVGs = accumulatedXiCs.Keys;
                    IEnumerable<VoltageGroup> toBeRemoved = VoltageGroupFilters.RemoveVoltageGroupsWithInsufficentFrames(remainingVGs, this.Parameters.InsufficientFramesFraction);
                    foreach (VoltageGroup voltageGroup in toBeRemoved)
                    {
                        accumulatedXiCs.Remove(voltageGroup);
                    }

                    // Perform feature detection and scoring and the given MzInDalton range on the accumulated XICs to get the base peaks.
                    if (detailedVerbose)
                    {
                        Trace.WriteLine("Feature detection and scoring: ");
                    }

                    IList<VoltageGroup> rejectedVoltageGroups = new List<VoltageGroup>();
                    IList<ObservedPeak> filteredObservations = new List<ObservedPeak>();
                    IList<ObservedPeak> allObservations = new List<ObservedPeak>();
                    IDictionary<string, IList<ObservedPeak>> rejectedObservations = new Dictionary<string, IList<ObservedPeak>>();

                    var numberOfParticipatingVGs = accumulatedXiCs.Keys.Count;

                    // Iterate through the features and perform filtering on isotopic affinity, intensity, drift time and peak shape.
                    foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                    {    
                        double globalMaxIntensity = IMSUtil.MaxIntensityAfterFrameAccumulation(voltageGroup, this.uimfReader);
                    
                        List<StandardImsPeak> standardPeaks = this.FindPeaksBasedOnXIC(voltageGroup, accumulatedXiCs[voltageGroup], target);
                        
                        // Score features
                        IDictionary<StandardImsPeak, PeakScores> scoresTable = new Dictionary<StandardImsPeak, PeakScores>();
                        if (detailedVerbose)
                        {
                            Trace.WriteLine(
                                string.Format(
                                    "    Voltage group: {0:F2} V, Frame {1}-{2}, {3:F2}K, {4:F2}Torr",
                                    voltageGroup.MeanVoltageInVolts,
                                    voltageGroup.FirstFrameNumber, 
                                    voltageGroup.LastFrameNumber,
                                    voltageGroup.MeanTemperatureInKelvin,
                                    voltageGroup.MeanPressureInTorr));

                        }

                        foreach (StandardImsPeak peak in standardPeaks)
                        {   
                            PeakScores currentStatistics = FeatureScoreUtilities.ScoreFeature(
                                peak, 
                                globalMaxIntensity,
                                this.uimfReader,
                                this.Parameters.MzWindowHalfWidthInPpm,
                                this.Parameters.DriftTimeToleranceInMs,
                                voltageGroup,
                                this.NumberOfScans,
                                target,
                                IsotopicScoreMethod.Angle, 
                                theoreticalIsotopicProfilePeakList);
                    
                            scoresTable.Add(peak, currentStatistics);
                        }

                        double maxIntensity = standardPeaks.Max(x => x.SummedIntensities);
                    
                        Predicate<StandardImsPeak> relativeIntensityThreshold = imsPeak => FeatureFilters.FilterOnRelativeIntesity(imsPeak, maxIntensity, this.Parameters.RelativeIntensityPercentageThreshold);

                        // filter out non Target peaks and noise. 
                        Predicate<StandardImsPeak> absoluteIntensityThreshold = imsPeak => FeatureFilters.FilterOnAbsoluteIntensity(imsPeak, scoresTable[imsPeak].IntensityScore, this.Parameters.AbsoluteIntensityThreshold);
                    
                        // filter out features with Ims scans at 1% left or right.
                        Predicate<StandardImsPeak> scanPredicate = imsPeak => FeatureFilters.FilterExtremeDriftTime(imsPeak, this.NumberOfScans);

                        // filter out features with bad peak shapes.
                        Predicate<StandardImsPeak> shapeThreshold = imsPeak => FeatureFilters.FilterBadPeakShape(imsPeak, scoresTable[imsPeak].PeakShapeScore, this.Parameters.PeakShapeThreshold);

                        // filter out features with distant isotopic profile.
                        Predicate<StandardImsPeak> isotopeThreshold = imsPeak => FeatureFilters.FilterBadIsotopicProfile(imsPeak, scoresTable[imsPeak].IsotopicScore, this.Parameters.IsotopicThreshold);
                    
                        // Print out candidate features and how they were rejected.
                        foreach (StandardImsPeak peak in standardPeaks)
                        {
                            PeakScores currentStatistics = scoresTable[peak];
                            string rejectionReason = ReportFeatureEvaluation(
                                peak,
                                currentStatistics,
                                detailedVerbose, 
                                target, 
                                scanPredicate(peak),
                                absoluteIntensityThreshold(peak),
                                relativeIntensityThreshold(peak),
                                shapeThreshold(peak),
                                isotopeThreshold(peak));   
                            bool pass = string.IsNullOrEmpty(rejectionReason);
                         
                            ObservedPeak analyzedPeak = new ObservedPeak(voltageGroup, peak, currentStatistics);
                            if (pass)
                            {
                                filteredObservations.Add(analyzedPeak);
                            }
                            else
                            {
                                if (rejectedObservations.ContainsKey(rejectionReason))
                                {
                                    rejectedObservations[rejectionReason].Add(analyzedPeak);
                                }

                                else
                                {
                                    rejectedObservations.Add(rejectionReason, new List<ObservedPeak>(){analyzedPeak});
                                }
                            }

                            allObservations.Add(analyzedPeak);
                        }
                        
                        standardPeaks.RemoveAll(scanPredicate);
                    
                        standardPeaks.RemoveAll(absoluteIntensityThreshold);

                        standardPeaks.RemoveAll(relativeIntensityThreshold);
                    
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

                    IEnumerable<ObservedPeak> rejectedPeaks = rejectedObservations.Values.SelectMany(x => x);

                    // Report analysis as negative
                    if (accumulatedXiCs.Keys.Count == 0)
                    {
                        CrossSectionWorkflowResult informedResult = CrossSectionWorkflowResult.CreateNegativeResult(rejectedPeaks, rejectedVoltageGroups, target, this.DatasetPath, this.OutputPath, this.SampleCollectionDate);
                        ReportAnslysisResultAndMetrics(informedResult, detailedVerbose);
                        return informedResult;
                    }
                    else
                    {
                    // Perform the data association algorithm.
                    IIonTracker tracker = new CombinatorialIonTracker(3000);

                    // Because for somereason we are not keeping track of drift tube length in UIMF...so we kind of have to go ask the instrument operator..
                    double driftTubeLength = this.Parameters.DriftTubeLengthInCm;
                    AssociationHypothesis optimalAssociationHypothesis = tracker.FindOptimumHypothesis(filteredObservations, driftTubeLength, target, this.Parameters, numberOfParticipatingVGs);

                    if (optimalAssociationHypothesis == null)
                    {
                        CrossSectionWorkflowResult negResult = CrossSectionWorkflowResult.CreateNegativeResult(rejectedPeaks, rejectedVoltageGroups, target, this.DatasetPath, this.OutputPath, this.SampleCollectionDate);
                        ReportAnslysisResultAndMetrics(negResult, detailedVerbose);
                        return negResult;
                    }

                    if (detailedVerbose)
                    {
                        Console.WriteLine("Writes QC plot of fitline to " + this.OutputPath);
                        Trace.WriteLine(string.Empty);
                    }
                    
                    string extension = (this.Parameters.GraphicsExtension.StartsWith(".")) ?
                        this.Parameters.GraphicsExtension : "." + this.Parameters.GraphicsExtension;
                    string outputPath = string.Format("{0}target_{1}_in_{2}_QA{3}", this.OutputPath, target.TargetDescriptor, this.DatasetName, extension);
                    ImsInformedPlotter plotter = new ImsInformedPlotter();
                    plotter.PlotAssociationHypothesis(optimalAssociationHypothesis, outputPath, this.DatasetName, target, rejectedObservations);

                    // Printout results
                    if (detailedVerbose)
                    {
                        Trace.WriteLine("Target Data Association");
                        int count = 0;
                        foreach (IsomerTrack track in optimalAssociationHypothesis.Tracks)
                        {
                            Trace.WriteLine(string.Format(" T{0}:   ", count));
                            foreach (ObservedPeak peak in track.ObservedPeaks)
                            {
                                Trace.WriteLine(string.Format("       [td: {0:F4}ms, V: {1:F4}V, T: {2:F4}K, P: {3:F4}Torr]", 
                                    peak.Peak.PeakApex.DriftTimeCenterInMs,
                                    peak.VoltageGroup.MeanVoltageInVolts,
                                    peak.VoltageGroup.MeanTemperatureInKelvin,
                                    peak.VoltageGroup.MeanPressureInTorr));
                            }
                            count++;
                            Trace.WriteLine("");
                        }

                        Trace.WriteLine("");
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
                        optimalAssociationHypothesis, 
                        target,
                        accumulatedXiCs.Keys,
                        allObservations,
                        this.DatasetPath,
                        this.OutputPath,
                        this.SampleCollectionDate,
                        viperFriendlyMass
                        );
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
                    return CrossSectionWorkflowResult.CreateErrorResult(target, this.DatasetName, this.DatasetPath, this.OutputPath, this.SampleCollectionDate);
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
        private FileStream ResetTraceListenerToTarget(IImsTarget target, string datasetName)
        {
            Trace.Listeners.Clear();
            string targetResultFileName = Path.Combine(this.OutputPath, "target_" + target.TargetDescriptor + "_in_" + datasetName + ".txt");
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
                Trace.WriteLine(string.Format("    Average Voltage Group Stability Score: {0:F4}", informedResult.AverageVoltageGroupStability));

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
        /// <param name="lowAbsoluteIntensity">
        /// The low intensity.
        /// </param>
        /// <param name="lowRelativeIntensity"></param>
        /// <param name="badPeakShape">
        /// The bad peak shape.
        /// </param>
        /// <param name="lowIsotopicAffinity">
        /// The low isotopic affinity.
        /// </param>
        /// <returns>
        /// If the feature pass all the filters <see cref="bool"/>.
        /// </returns>
        private static string ReportFeatureEvaluation(StandardImsPeak peak, PeakScores scores, bool verbose, IImsTarget target, bool badScanRange, bool lowAbsoluteIntensity, bool lowRelativeIntensity, bool badPeakShape, bool lowIsotopicAffinity)
        {
            Trace.WriteLine(string.Format("        Candidate feature found at [centerMz = {0:F4}, drift time = {1:F2} ms(#{2})] ", peak.PeakApex.MzCenterInDalton, peak.PeakApex.DriftTimeCenterInMs,     peak.PeakApex.DriftTimeCenterInScanNumber));
            Trace.WriteLine(string.Format("            IntensityScore: {0:F4}", scores.IntensityScore));
            
            if (!lowAbsoluteIntensity)
            {
                Trace.WriteLine(string.Format("            peakShapeScore: {0:F4}", scores.PeakShapeScore));
            
                if (target.HasCompositionInfo)
                {
                    Trace.WriteLine(string.Format("            isotopicScore:  {0:F4}", scores.IsotopicScore));
                }
            }

            string rejectionReason = badScanRange ? "[Bad scan range] " : 
                lowAbsoluteIntensity ? "[Low Absolute Intensity] " :
                badPeakShape ? "[Bad Peak Shape] " : 
                lowIsotopicAffinity ? "[Different Isotopic Profile] " : 
                lowRelativeIntensity ? "[Low Relative Intensity] " : string.Empty;

            bool rejected = badScanRange || lowAbsoluteIntensity || lowIsotopicAffinity || badPeakShape || lowRelativeIntensity;

            if (verbose)
            {
                if (rejected)
                {
                    Trace.WriteLine("        " + rejectionReason);
                }
                else
                {
                    Trace.WriteLine("        [PASS]");
                }

                Trace.WriteLine(string.Empty);
            }

            return rejectionReason;
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
                    double driftTimeInMs = peak.PeakApex.DriftTimeCenterInMs;
                    
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
                                voltageGroup.FirstFrameNumber,
                                voltageGroup.LastFrameNumber));
                    
                        Trace.WriteLine(
                            string.Format(
                                "        Normalized Drift Time: {0:F4} ms (Scan# = {1})",
                                normalizedDriftTimeInMs,
                                peak.PeakApex.DriftTimeCenterInScanNumber));

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
                return featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, this.uimfReader, voltageGroup, target.MassWithAdduct, this.Parameters.MzWindowHalfWidthInPpm)).ToList();
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
                Trace.WriteLine("Target chemical: " + target.CorrespondingChemical);
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
                

                Trace.WriteLine(string.Format("    M/Z: {0:F4} Dalton({1:F4} ppm)", isomer.MzInDalton, isomer.MzInPpm));
                Trace.WriteLine(string.Format("    Intensity Score: {0:F4}", isomer.PeakScores.IntensityScore));
                Trace.WriteLine(string.Format("    Peak Shape Score: {0:F4})", isomer.PeakScores.PeakShapeScore));
                Trace.WriteLine(string.Format("    Isotopic Score: {0:F4}", isomer.PeakScores.IsotopicScore));
                Trace.WriteLine(string.Format("    R2: {0:F4}", isomer.RSquared));
                Trace.WriteLine(string.Format("    Mobility: {0:F4} cm^2/(s*V)", isomer.Mobility));
                Trace.WriteLine(string.Format("    T0: {0:F4} ms", isomer.T0));
                Trace.WriteLine(string.Format("    Cross Sectional Area: {0:F4} Å^2", isomer.CrossSectionalArea));
                ArrivalTimeSnapShot lastDriftTime = isomer.ArrivalTimeSnapShots.Last();

                Trace.WriteLine(string.Format("    Last VoltageGroup Drift Time: {0:F4} ms [V: {1:F2}V, T: {2:F2}K, P: {3:F2}Torr]", lastDriftTime.MeasuredArrivalTimeInMs, lastDriftTime.DriftTubeVoltageInVolt, lastDriftTime.TemperatureInKelvin, lastDriftTime.PressureInTorr));
                
                isomerIndex++;
            
                if (!onlyOneIsomer)
                {
                    Trace.WriteLine(string.Empty);
                }
            }
        }
    }
}
