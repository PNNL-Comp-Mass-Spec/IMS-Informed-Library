﻿// --------------------------------------------------------------------------------------------------------------------
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
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Filters;
    using ImsInformed.Interfaces;
    using ImsInformed.IO;
    using ImsInformed.Scoring;
    using ImsInformed.Stats;
    using ImsInformed.Targets;
    using ImsInformed.Util;
    using ImsInformed.Workflows.LcImsPeptideExtraction;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

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
        public readonly double NumberOfFrames;

        /// <summary>
        /// The number of scans.
        /// </summary>
        public readonly int NumberOfScans;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionWorkfow"/> class.
        /// </summary>
        /// <param name="uimfFileLocation">
        /// The UIMF file location.
        /// </param>
        /// <param name="outputDirectory">
        /// The output directory.
        /// </param>
        /// <param name="resultFileName">
        /// The result path.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public CrossSectionWorkfow(string uimfFileLocation, string outputDirectory, string resultFileName, CrossSectionSearchParameters parameters)
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

            this.ResultFileName = resultFileName;
            
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

            Trace.Listeners.Clear();
            ConsoleTraceListener consoleTraceListener = new ConsoleTraceListener(false);
            consoleTraceListener.TraceOutputOptions = TraceOptions.DateTime;
            string result = this.OutputPath + this.ResultFileName;
            this.resultFileWriter = File.AppendText(result);
            TextWriterTraceListener resultFileTraceListener = new TextWriterTraceListener(this.resultFileWriter)
            {
                Name = "this.DatasetName" + "_Result", 
                TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
            };
            
            Trace.Listeners.Add(consoleTraceListener);
            Trace.Listeners.Add(resultFileTraceListener);
            Trace.AutoFlush = true;
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
        /// The file writer.
        /// </summary>
        private readonly StreamWriter resultFileWriter; 
        

         /// <summary>
        /// The _peak detector.
        /// </summary>
        protected readonly ChromPeakDetector peakDetector;

        /// <summary>
        /// Gets the result path.
        /// </summary>
        public string ResultFileName { get; private set; }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// The target ion.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="n">
        /// The n.
        /// </param>
        /// <param name="chargeState">
        /// The charge state.
        /// </param>
        /// <returns>
        /// The <see cref="Ion"/>.
        /// </returns>
        public static Ion TargetIon(MolecularTarget target, int n, int chargeState)
        {
            return new Ion(target.Composition, chargeState);
        }

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
        /// The <see cref="IDictionary"/>.
        /// </returns>
        public IDictionary<string, CrossSectionWorkflowResult> RunCrossSectionWorkFlow(IEnumerable<IImsTarget> targetList, bool detailedVerbose = true)
        {
            IDictionary<string, CrossSectionWorkflowResult> targetResultMap = new Dictionary<string, CrossSectionWorkflowResult>();
            foreach (IImsTarget target in targetList)
            {
                if (target.EmpiricalFormula != null)
                {
                    Console.Write("    Target: " + target.EmpiricalFormula);
                    Console.WriteLine(" (MZ = {0})", target.Mass);
                }
                else
                {
                    Console.WriteLine("    Target: Unknown (MZ = {0})", target.TargetMz);
                }

                CrossSectionWorkflowResult result = this.RunCrossSectionWorkFlow(target, detailedVerbose);
                targetResultMap.Add(target.TargetDescriptor, result);
            }

            return targetResultMap;
        }

        /// <summary>
        /// The run molecule informed work flow.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="detailedVerbose">
        /// </param>
        /// <returns>
        /// The <see cref="CrossSectionWorkflowResult"/>.
        /// </returns>
        public CrossSectionWorkflowResult RunCrossSectionWorkFlow(IImsTarget target, bool detailedVerbose = true)
        {
            string targetDescription = target.TargetDescriptor;
            double monoisotopicMass = 0;

            CrossSectionWorkflowResult informedResult;

            try
            {
                // Compensate for mass changes due to ionization
                Composition targetComposition = MoleculeUtil.IonizationCompositionCompensation(target.Composition, target.IonizationType);

                // Get the monoisotopic mass for viper
                double targetMass = (target.Composition == null) ? target.TargetMz : targetComposition.Mass;
                if (target.IonizationType == IonizationMethod.ProtonMinus || target.IonizationType == IonizationMethod.APCI || target.IonizationType == IonizationMethod.HCOOMinus || target.IonizationType == IonizationMethod.Proton2MinusSodiumPlus)
                {
                    monoisotopicMass = targetMass + new Composition(0, 1, 0, 0, 0).Mass;
                }
                else
                {
                    monoisotopicMass = targetMass - new Composition(0, 1, 0, 0, 0).Mass;
                }
                
                // ImsTarget assumes proton+ ionization because it's designed for peptides. Get rid of it here.
                targetComposition = MoleculeUtil.IonizationCompositionDecompensation(targetComposition, IonizationMethod.ProtonPlus);
                             
                // Setup target object
                if (targetComposition != null) 
                {
                    // Because Ion class from Informed Proteomics assumes adding a proton, that's the reason for decompensation
                    Ion targetIon = new Ion(targetComposition, 1);
                    target.TargetMz = targetIon.GetMonoIsotopicMz();
                } 
                
                Trace.WriteLine(string.Empty);

                if (detailedVerbose)
                {
                    Trace.WriteLine("Dataset: " + this.DatasetName);
                    Trace.WriteLine("Ionization method: " + target.IonizationType.ToFriendlyString());
                    if (targetComposition != null)
                    {
                        Trace.WriteLine("Target Empirical Formula: " + target.EmpiricalFormula);
                    }

                    Trace.WriteLine("Targeting Mz: " + target.TargetMz);
                    Trace.WriteLine(string.Empty);
                } 
                else
                {
                    if (targetComposition == null)
                    {
                        Trace.Write(string.Format("Target: Mz = {0}", target.TargetMz));
                    }
                    else
                    {
                        Trace.Write(string.Format("Target: {0}", target.EmpiricalFormula));
                    }

                    Trace.WriteLine(target.IonizationType.ToFriendlyString() + ":");
                }

                // Generate Theoretical Isotopic Profile
                List<Peak> theoreticalIsotopicProfilePeakList = null;
                if (targetComposition != null) 
                {
                    string empiricalFormula = targetComposition.ToPlainString();
                    IsotopicProfile theoreticalIsotopicProfile = this.theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, 1);
                    theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
                }
                
                // Voltage grouping
                VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(this.uimfReader, target.TargetMz, this.Parameters);

                // For each voltage, find 2D XIC features 
                if (detailedVerbose)
                {
                    Trace.WriteLine("Feature detection and scoring: ");
                }

                IList<VoltageGroup> rejectionList = new List<VoltageGroup>();
                foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                {    
                    double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, this.uimfReader);
                
                    List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;

                    PeakFinding.FindPeakUsingMasic(intensityPoints, this.NumberOfScans);
                    List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, this.smoother, this.Parameters);
                
                    // Calculate feature statistics and discard features with 
                    foreach (FeatureBlob feature in featureBlobs)
                    {
                        feature.CalculateStatistics();
                    }
                
                    // Score features
                    IDictionary<FeatureBlob, FeatureScoreHolder> scoresTable = new Dictionary<FeatureBlob, FeatureScoreHolder>();
                    if (detailedVerbose)
                    {
                        Trace.WriteLine(string.Format("    Voltage Group: {0:F4} V, [{1}-{2}]", voltageGroup.MeanVoltageInVolts, voltageGroup.FirstFrameNumber - 1, voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount - 2));
                    }

                    foreach (FeatureBlob featureBlob in featureBlobs)
                    {   
                        FeatureScoreHolder currentScoreHolder;
                        currentScoreHolder.IntensityScore = FeatureScores.IntensityScore(featureBlob, voltageGroup, globalMaxIntensity);
                        
                        currentScoreHolder.PeakShapeScore = FeatureScores.PeakShapeScore(this.uimfReader, this.Parameters, featureBlob.Statistics, voltageGroup, target.TargetMz, globalMaxIntensity, (int)this.NumberOfScans);
                
                        currentScoreHolder.IsotopicScore = 0;
                        if (targetComposition != null)
                        {
                            currentScoreHolder.IsotopicScore = FeatureScores.IsotopicProfileScore(this.uimfReader, this.Parameters, target, featureBlob.Statistics, theoreticalIsotopicProfilePeakList, voltageGroup, IsotopicScoreMethod.Angle, globalMaxIntensity, (int)this.NumberOfScans);
                        }
                
                        scoresTable.Add(featureBlob, currentScoreHolder);
                    }
                
                    // 2st round filtering: filter out non target peaks and noise. 
                    Predicate<FeatureBlob> intensityThreshold = blob => FeatureFilters.FilterLowIntensity(blob, scoresTable[blob].IntensityScore, this.Parameters.IntensityThreshold);
                
                    // filter out features with Ims scans at 1% left or right.
                    Predicate<FeatureBlob> scanPredicate = blob => FeatureFilters.FilterExtremeDriftTime(blob, (int)this.NumberOfScans);
                    Predicate<FeatureBlob> shapeThreshold = blob => FeatureFilters.FilterBadPeakShape(blob, scoresTable[blob].PeakShapeScore, this.Parameters.PeakShapeThreshold);
                    Predicate<FeatureBlob> isotopeThreshold = blob => FeatureFilters.FilterBadIsotopicProfile(blob, scoresTable[blob].IsotopicScore, this.Parameters.IsotopicThreshold);
                
                    // Print out candidate features that pass the intensity threshold.
                    foreach (FeatureBlob featureBlob in featureBlobs)
                    {  
                        bool badScanRange = scanPredicate(featureBlob);
                        bool lowIntenstity = intensityThreshold(featureBlob);
                        bool badPeakShape = shapeThreshold(featureBlob);
                        bool lowIsotopicAffinity = isotopeThreshold(featureBlob);
                        FeatureScoreHolder currentScoreHolder = scoresTable[featureBlob];
                        if (detailedVerbose)
                        {
                            Trace.WriteLine(string.Format("        Candidate feature found at scan number {0}", featureBlob.Statistics.ScanImsRep));
                            Trace.WriteLine(string.Format("            IntensityScore: {0:F4}", currentScoreHolder.IntensityScore));
                            if (!lowIntenstity)
                            {
                                Trace.WriteLine(string.Format("            peakShapeScore: {0:F4}", currentScoreHolder.PeakShapeScore));
                            
                                if (targetComposition != null)
                                {
                                    Trace.WriteLine(string.Format("            isotopicScore:  {0:F4}", currentScoreHolder.IsotopicScore));
                                }
                            }
                        }
                
                        string rejectionReason = badScanRange ? "        [Bad scan range] " : "        ";
                        rejectionReason += lowIntenstity ? "[Low Intensity] " : string.Empty;
                        rejectionReason += !lowIntenstity && badPeakShape ? "[Bad Peak Shape] " : string.Empty;
                        rejectionReason += !lowIntenstity && lowIsotopicAffinity ? "[Different Isotopic Profile] " : string.Empty;

                        if (detailedVerbose)
                        {
                            if (badScanRange || lowIntenstity || lowIsotopicAffinity || badPeakShape)
                            {
                                Trace.WriteLine(rejectionReason);
                            }
                            else
                            {
                                Trace.WriteLine("        [PASS]");
                            }
                
                            Trace.WriteLine(string.Empty);
                        }
                    }
                    
                    featureBlobs.RemoveAll(scanPredicate);
                
                    featureBlobs.RemoveAll(intensityThreshold);
                
                    featureBlobs.RemoveAll(shapeThreshold);
                    if (targetComposition != null)
                    {
                        featureBlobs.RemoveAll(isotopeThreshold);
                    }
                
                    ScoreUtil.LikelihoodFunc likelihoodFunc = TargetPresenceLikelihoodFunctions.IntensityDominantLikelihoodFunction;
                    if (featureBlobs.Count > 0)
                    {
                        IDictionary<FeatureBlob, FeatureScoreHolder> qualifiedFeatures =
                            featureBlobs.ToDictionary(feature => feature, feature => scoresTable[feature]);

                        // Assign the best feature to voltage group it belongs to, with the feature score as one of the criteria of that voltage group.

                        // TODO: If there are more than one apexes in the best feature. Treat them as isomers.
                        voltageGroup.BestFeature = ScoreUtil.SelectMostLikelyFeature(qualifiedFeatures, likelihoodFunc);
                        voltageGroup.BestFeatureScores = scoresTable[voltageGroup.BestFeature];
                    }
                    else 
                    {
                        if (detailedVerbose)
                        {
                            Trace.WriteLine(string.Format("    (All features were rejected in voltage group {0:F4} V)", voltageGroup.MeanVoltageInVolts));
                            Trace.WriteLine(string.Empty);
                            Trace.WriteLine(string.Empty);
                        }
                
                        // Select the one of the better features from the features rejected to represent the voltage group.
                        voltageGroup.BestFeature = ScoreUtil.SelectMostLikelyFeature(scoresTable, likelihoodFunc);
                        if (voltageGroup.BestFeature != null)
                        {
                            voltageGroup.BestFeatureScores = scoresTable[voltageGroup.BestFeature];
                        }
                
                        rejectionList.Add(voltageGroup);
                    }
                
                    // Rate the feature's VoltageGroupScore score. VoltageGroupScore score measures how likely the voltage group contains and detected the target ion.
                    voltageGroup.VoltageGroupScore = VoltageGroupScore.ComputeVoltageGroupStabilityScore(voltageGroup);
                }
                
                // Remove voltage groups
                foreach (VoltageGroup voltageGroup in rejectionList)
                {
                    accumulatedXiCs.Remove(voltageGroup);

                    // Choose the best feature in the voltage group as the best feature (even if non qualified).
                }

                if (accumulatedXiCs.Keys.Count < 1)
                {
                    AnalysisScoresHolder analysisScores;
                    analysisScores.RSquared = 0;
                    analysisScores.AverageVoltageGroupStabilityScore = VoltageGroupScore.AverageVoltageGroupStabilityScore(rejectionList);
                    
                    // quantize the VG score from VGs in the removal list.
                    IEnumerable<FeatureScoreHolder> featureScores = rejectionList.Select(x => x.BestFeatureScores);
                    analysisScores.AverageCandidateTargetScores = FeatureScores.AverageFeatureScores(featureScores);
                    informedResult = new CrossSectionWorkflowResult(
                        this.DatasetName, 
                        targetDescription, 
                        target.IonizationType, 
                        AnalysisStatus.Negative, 
                        analysisScores, 
                        null);
                    
                    Trace.WriteLine("Analysis result");
                    Trace.WriteLine(string.Format("    Analysis Conclusion: {0}", informedResult.AnalysisStatus));
                    if (detailedVerbose)
                    {
                        Trace.WriteLine(string.Format("    Average Voltage Group Stability Score {0:F4}", informedResult.AnalysisScoresHolder.AverageVoltageGroupStabilityScore));
                        Trace.WriteLine(string.Format("    Average Best Feature Intensity Score {0:F4}",    informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.IntensityScore));
                        
                        if (targetComposition != null)
                        {
                            Trace.WriteLine(string.Format("    Average Best Feature Isotopic Score {0:F4}",     informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.IsotopicScore));
                        }

                        Trace.WriteLine(string.Format("    Average Best Feature Peak Shape Score {0:F4}", informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.PeakShapeScore));
                    }

                    return informedResult;
                }
                
                // Calculate the fit line from the remaining voltage groups with reliable drift time measurement.
                HashSet<ContinuousXYPoint> allFitPoints = new HashSet<ContinuousXYPoint>();
                foreach (VoltageGroup group in accumulatedXiCs.Keys)
                {
                    // convert drift time to SI unit seconds
                    double x = group.BestFeature.Statistics.ScanImsRep * group.AverageTofWidthInSeconds;
                
                    // P/(T*V) value in pascal per (volts * kelvin)
                    double y = group.MeanPressureNondimensionalized / group.MeanVoltageInVolts
                               / group.MeanTemperatureNondimensionalized;
                     
                    ContinuousXYPoint point = new ContinuousXYPoint(x, y);
                
                    allFitPoints.Add(point);
                
                    // Add fit point to voltage group
                    group.FitPoint = point;
                }
                
                double driftTubeLength = FakeUIMFReader.DriftTubeLengthInCentimeters;
                FitLine line = new FitLine(allFitPoints);
                
                double voltageGroupDriftTimeInMs = -1;
                
                // Printout results
                if (detailedVerbose)
                {
                    Trace.WriteLine("Target Identification");
                }

                foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                {
                    voltageGroupDriftTimeInMs = voltageGroup.FitPoint.X * 1000;
                
                    // Normalize the drift time to be displayed.
                    voltageGroupDriftTimeInMs = MoleculeUtil.NormalizeDriftTime(voltageGroupDriftTimeInMs, voltageGroup);
                    if (detailedVerbose)
                    {
                        Trace.WriteLine(string.Format("    Target presence confirmed at {0:F2} ± {1:F2} V.", voltageGroup.MeanVoltageInVolts, Math.Sqrt (voltageGroup.VarianceVoltage)));
                        Trace.WriteLine(string.Format("        Frame range: [{0}, {1}]", voltageGroup.FirstFrameNumber - 1,     voltageGroup.FirstFrameNumber   +voltageGroup.AccumulationCount - 2));
                        Trace.WriteLine(string.Format("        Drift time: {0:F4} ms (Scan# = {1})", voltageGroup.FitPoint.X * 1000,    voltageGroup.BestFeature.Statistics.ScanImsRep));
                                                               
                        Trace.WriteLine(string.Format("        Cook's distance: {0:F4}", voltageGroup.FitPoint.CooksD));
                        Trace.WriteLine(string.Format("        VoltageGroupScore: {0:F4}", voltageGroup.VoltageGroupScore));
                        Trace.WriteLine(string.Format("        IntensityScore: {0:F4}", voltageGroup.BestFeatureScores.IntensityScore));
                        if (targetComposition != null)
                        {
                            Trace.WriteLine(string.Format("        IsotopicScore: {0:F4}", voltageGroup.BestFeatureScores.IsotopicScore));
                        }
                
                        Trace.WriteLine(string.Format("        PeakShapeScore: {0:F4}", voltageGroup.BestFeatureScores.PeakShapeScore));
                        Trace.WriteLine(string.Empty);
                    }
                }
                
                // If not enough points
                int minFitPoints = this.Parameters.MinFitPoints;
                bool sufficientPoints = allFitPoints.Count >= minFitPoints;
                if (!sufficientPoints)
                {
                    if (detailedVerbose)
                    {
                        Trace.WriteLine("Not enough points are qualified to perform linear fit. Abort identification.");
                    }
                
                    AnalysisScoresHolder scoreHolder;
                    scoreHolder.RSquared = 0;
                    scoreHolder.AverageVoltageGroupStabilityScore = VoltageGroupScore.AverageVoltageGroupStabilityScore(accumulatedXiCs.Keys);
                    IEnumerable<FeatureScoreHolder> featureScores = accumulatedXiCs.Keys.Select(x => x.BestFeatureScores);
                    scoreHolder.AverageCandidateTargetScores = FeatureScores.AverageFeatureScores(featureScores);
                    informedResult = new CrossSectionWorkflowResult(
                        this.DatasetName, 
                        targetDescription, 
                        target.IonizationType, 
                        AnalysisStatus.NotSufficientPoints, 
                        scoreHolder, 
                        null);
                    return informedResult;
                }
                
                // Remove outliers with high influence.
                line.RemoveOutliersAboveThreshold(3, minFitPoints);
                
                // Remove outliers until min fit point is reached or good R2 is achieved.
                while (AnalysisFilter.FilterLowR2(line.RSquared) && line.FitPointCollection.Count > minFitPoints)
                {
                    line.RemoveOutlierWithHighestCookDistance(minFitPoints);
                }
                
                // Remove the voltage considered outliers
                foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys.Where(p => line.OutlierCollection.Contains(p.FitPoint)).ToList())
                {
                    accumulatedXiCs.Remove(voltageGroup);
                }
                    
                // Export the fit line into QC oxyplot drawings
                string outputPath = this.OutputPath + this.DatasetName + "_" + target.IonizationType + "_QA.png";
                ImsInformedPlotter.MobilityFitLine2PNG(outputPath, line);
                if (detailedVerbose)
                {
                    Console.WriteLine("Writes QC plot of fitline to " + outputPath);
                    Trace.WriteLine(string.Empty);
                }
                
                double rSquared = line.RSquared;
                
                // Compute mobility and cross section area
                double mobility = driftTubeLength * driftTubeLength / (1 / line.Slope);
                Composition bufferGas = new Composition(0, 0, 2, 0, 0);
                double reducedMass = MoleculeUtil.ComputeReducedMass(target.TargetMz, bufferGas);
                
                // Find the average temperature across various non outlier voltage groups.
                double globalMeanTemperature = 0;
                int frameCount = 0;
                foreach (VoltageGroup group in accumulatedXiCs.Keys)
                {
                    double voltageGroupTemperature = UnitConversion.AbsoluteZeroInKelvin * group.MeanTemperatureNondimensionalized;
                    globalMeanTemperature += voltageGroupTemperature * group.AccumulationCount;
                    frameCount += group.AccumulationCount;
                }
                
                globalMeanTemperature /= frameCount;
                
                double crossSection = MoleculeUtil.ComputeCrossSectionalArea(globalMeanTemperature, mobility, 1, reducedMass); // Charge State is assumed to be 1 here;
                
                // Initialize the result struct.
                AnalysisScoresHolder analysisScoreHolder;
                analysisScoreHolder.RSquared = rSquared;
                analysisScoreHolder.AverageVoltageGroupStabilityScore = VoltageGroupScore.AverageVoltageGroupStabilityScore(accumulatedXiCs.Keys);
                IEnumerable<FeatureScoreHolder> scores = accumulatedXiCs.Keys.Select(x => x.BestFeatureScores);
                analysisScoreHolder.AverageCandidateTargetScores = FeatureScores.AverageFeatureScores(scores);
                AnalysisStatus finalStatus = AnalysisFilter.FilterLowR2(rSquared) ? AnalysisStatus.Rejected : AnalysisStatus.Positive;
                
                // Check if the last voltage group still exists as fit point
                if (finalStatus == AnalysisStatus.Rejected)
                {
                    voltageGroupDriftTimeInMs = -1;
                }
                
                // Check if the last voltage remaining is the last voltage group in the experiment.
                if (accumulatedXiCs.Keys.Last().FirstFrameNumber + accumulatedXiCs.Keys.Last().AccumulationCount - 2 < this.NumberOfFrames - 1)
                {
                    voltageGroupDriftTimeInMs = -2;
                }
                
                IList<TargetIsomerReport> isomers = new List<TargetIsomerReport>();
                TargetIsomerReport mostLikelyIsomer;
                mostLikelyIsomer.Mobility = mobility;
                mostLikelyIsomer.CrossSectionalArea = crossSection;
                mostLikelyIsomer.MonoisotopicMass = monoisotopicMass;
                mostLikelyIsomer.LastVoltageGroupDriftTimeInMs = voltageGroupDriftTimeInMs;
                isomers.Add(mostLikelyIsomer);
                
                informedResult = new CrossSectionWorkflowResult(
                this.DatasetName, 
                targetDescription, 
                target.IonizationType, 
                finalStatus, 
                analysisScoreHolder, 
                isomers);
                
                Trace.WriteLine("Analysis result");
                Trace.WriteLine(string.Format("    Analysis Conclusion: {0}", informedResult.AnalysisStatus));
                Trace.WriteLine(string.Format("    R Squared {0:F4}", informedResult.AnalysisScoresHolder.RSquared));

                if (detailedVerbose)
                {
                    Trace.WriteLine(string.Format("    Average Voltage Group Stability Score {0:F4}", informedResult.AnalysisScoresHolder.AverageVoltageGroupStabilityScore));
                    Trace.WriteLine(string.Format("    Average Candidate Target Intensity Score {0:F4}",    informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.IntensityScore));
                    
                    if (targetComposition != null)
                    {
                        Trace.WriteLine(string.Format("    Average Candidate Target Isotopic Score {0:F4}",     informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.IsotopicScore));
                    }
                    
                    Trace.WriteLine(string.Format("    Average Candidate Target Peak Shape Score {0:F4}", informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.PeakShapeScore));
                }
                
                int isomerIndex = 1;
                bool onlyOneIsomer = informedResult.MatchingIsomers.Count() <= 1;
                foreach (TargetIsomerReport isomer in informedResult.MatchingIsomers)
                {
                    if (!onlyOneIsomer)
                    {
                        Trace.WriteLine(string.Format("    Isomer #[{0}]" , isomerIndex));
                    }
                
                    Trace.WriteLine(string.Format("    Last VoltageGroup Drift Time: {0:F4} ms", isomer.LastVoltageGroupDriftTimeInMs));
                    Trace.WriteLine(string.Format("    Mobility: {0:F4} cm^2/(s*V)", isomer.Mobility));
                    Trace.WriteLine(string.Format("    Cross Sectional Area: {0:F4} Å^2", isomer.CrossSectionalArea));
                    isomerIndex++;
                
                    if (!onlyOneIsomer)
                    {
                        Trace.WriteLine(string.Empty);
                    }
                }
                
                return informedResult;
            }
            catch (Exception e)
            {
                // Print result
                Trace.Listeners.Clear();
                ConsoleTraceListener consoleTraceListener = new ConsoleTraceListener(false);
                consoleTraceListener.TraceOutputOptions = TraceOptions.DateTime;
                string result = this.OutputPath + this.ResultFileName;
                
                TextWriterTraceListener resultFileTraceListener = new TextWriterTraceListener(this.resultFileWriter)
                {
                    Name = "this.DatasetName" + "_Result", 
                    TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
                };
                
                Trace.Listeners.Add(consoleTraceListener);
                Trace.Listeners.Add(resultFileTraceListener);
                Trace.AutoFlush = true;
                Trace.WriteLine(e.Message);
                Trace.WriteLine(e.StackTrace);

                // create the error result
                AnalysisScoresHolder analysisScores;
                analysisScores.RSquared = 0;
                analysisScores.AverageCandidateTargetScores.IntensityScore = 0;
                analysisScores.AverageCandidateTargetScores.IsotopicScore = 0;
                analysisScores.AverageCandidateTargetScores.PeakShapeScore = 0;
                analysisScores.AverageVoltageGroupStabilityScore = 0;

                informedResult = new CrossSectionWorkflowResult(
                    this.DatasetName, 
                    targetDescription, 
                    target.IonizationType, 
                    AnalysisStatus.UknownError, 
                    analysisScores, 
                    null);

                return informedResult;
            }
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
                this.resultFileWriter.Close();
                this.DatasetName = null;
                this.OutputPath = null;
            }

            // free native resources if there are any.
            Trace.Listeners.Clear();
        }
    }
}