// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoleculeInformedWorkflow.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Find molecules with a known formula and know ionization methods. metabolites and pipetides alike.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using DeconTools.Backend.Core;

    using ImsInformed.Domain;
    using ImsInformed.Filters;
    using ImsInformed.IO;
    using ImsInformed.Parameters;
    using ImsInformed.Scoring;
    using ImsInformed.Stats;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// Find molecules with a known formula and know ionization methods. metabolites and pipetides alike.
    /// </summary>
    public class MoleculeInformedWorkflow : InformedWorkflow, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MoleculeInformedWorkflow"/> class.
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
        public MoleculeInformedWorkflow(string uimfFileLocation, string outputDirectory, string resultFileName, MoleculeWorkflowParameters parameters) : base(uimfFileLocation, parameters)
        {
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
            this.fileWriter = File.AppendText(result);
            TextWriterTraceListener resultFileTraceListener = new TextWriterTraceListener(fileWriter)
            {
                Name = "this.DatasetName" + "_Result",
                TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
            };
            
            Trace.Listeners.Add(consoleTraceListener);
            Trace.Listeners.Add(resultFileTraceListener);
            Trace.AutoFlush = true;
        }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public MoleculeWorkflowParameters Parameters { get; set; }

        /// <summary>
        /// Gets or sets the dataset name.
        /// </summary>
        public string DatasetName { get; set; }

        /// <summary>
        /// The file writer.
        /// </summary>
        private readonly StreamWriter fileWriter; 

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
        public static Ion TargetIon(ImsTarget target, int n, int chargeState)
        {
            return new Ion(target.Composition, chargeState);
        }

        public IDictionary<string, MoleculeInformedWorkflowResult> RunMoleculeInformedWorkFlow(IEnumerable<ImsTarget> targetList, bool detailedVerbose = true)
        {
            IDictionary<string, MoleculeInformedWorkflowResult> targetResultMap = new Dictionary<string, MoleculeInformedWorkflowResult>();
            foreach (var target in targetList)
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

                MoleculeInformedWorkflowResult result = this.RunMoleculeInformedWorkFlow(target, detailedVerbose);
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
        /// <param name="detailedVerbose"></param>
        /// <returns>
        /// The <see cref="MoleculeInformedWorkflowResult"/>.
        /// </returns>
        public MoleculeInformedWorkflowResult RunMoleculeInformedWorkFlow(ImsTarget target, bool detailedVerbose = true)
        {
            string targetDescription = target.TargetDescriptor;
            double monoisotopicMass = 0;

            MoleculeInformedWorkflowResult informedResult;

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
                
                Trace.WriteLine(String.Empty);

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
                        Trace.Write(String.Format("Target: Mz = {0}", target.TargetMz));
                    }
                    else
                    {
                        Trace.Write(String.Format("Target: {0}", target.EmpiricalFormula));
                    }

                    Trace.WriteLine(target.IonizationType.ToFriendlyString() + ":");
                }

                // Generate Theoretical Isotopic Profile
                List<Peak> theoreticalIsotopicProfilePeakList = null;
                if (targetComposition != null) 
                {
                    string empiricalFormula = targetComposition.ToPlainString();
                    IsotopicProfile theoreticalIsotopicProfile = _theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, 1);
                    theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
                }
                
                // Voltage grouping
                VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(_uimfReader, target.TargetMz, _parameters);
                
                // For each voltage, find 2D XIC features 
                if (detailedVerbose)
                {
                    Trace.WriteLine("Feature detection and scoring: ");
                }

                IList<VoltageGroup> rejectionList = new List<VoltageGroup>();
                foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                {    
                    double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, _uimfReader);
                
                    // Smooth Chromatogram
                    IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(accumulatedXiCs[voltageGroup].IntensityPoints);
                    _smoother.Smooth(ref pointList);
                    
                    // Peak Find Chromatogram
                    List<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList).ToList();
                
                    // Preliminary filtering: reject small feature peaks.
                    featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, this.Parameters.FeatureFilterLevel).ToList();
                
                    // Calculate feature statistics and discard features with 
                    foreach (FeatureBlob feature in featureBlobs)
                    {
                        feature.CalculateStatistics();
                    }
                
                    // Score features
                    IDictionary<FeatureBlob, FeatureScoreHolder> scoresTable = new Dictionary<FeatureBlob, FeatureScoreHolder>();
                    if (detailedVerbose)
                    {
                        Trace.WriteLine(String.Format("    Voltage Group: {0:F4} V, [{1}-{2}]", voltageGroup.MeanVoltageInVolts, voltageGroup.FirstFrameNumber - 1, voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount - 2));
                    }

                    foreach (var featureBlob in featureBlobs)
                    {   
                        FeatureScoreHolder currentScoreHolder;
                        currentScoreHolder.IntensityScore = FeatureScores.IntensityScore(this, featureBlob, voltageGroup, globalMaxIntensity);
                        
                        currentScoreHolder.PeakShapeScore = FeatureScores.PeakShapeScore(this, featureBlob.Statistics, voltageGroup, target.TargetMz, globalMaxIntensity);
                
                        currentScoreHolder.IsotopicScore = 0;
                        if (targetComposition != null)
                        {
                             currentScoreHolder.IsotopicScore = FeatureScores.IsotopicProfileScore(this, target,featureBlob.Statistics,theoreticalIsotopicProfilePeakList,voltageGroup,IsotopicScoreMethod.Angle, globalMaxIntensity);
                        }
                
                        scoresTable.Add(featureBlob, currentScoreHolder);
                    }
                
                    // 2st round filtering: filter out non target peaks and noise. 
                    Predicate<FeatureBlob> intensityThreshold = blob => FeatureFilters.FilterLowIntensity(blob, scoresTable[blob].IntensityScore, this.Parameters.IntensityThreshold);
                
                    // filter out features with Ims scans at 1% left or right.
                    Predicate<FeatureBlob> scanPredicate = blob => FeatureFilters.FilterExtremeDriftTime(blob, (int)this.NumberOfScans);
                    Predicate<FeatureBlob> shapeThreshold = blob => FeatureFilters.FilterBadPeakShape(blob, scoresTable[blob].PeakShapeScore, this.Parameters.PeakShapeThreshold);
                    Predicate<FeatureBlob> isotopeThreshold = blob => FeatureFilters.FilterBadIsotopicProfile(blob, scoresTable[blob].IsotopicScore, this.Parameters.IsotopicFitScoreThreshold);
                
                    // Print out candidate features that pass the intensity threshold.
                    foreach (var featureBlob in featureBlobs)
                    {  
                        bool badScanRange = scanPredicate(featureBlob);
                        bool lowIntenstity = intensityThreshold(featureBlob);
                        bool badPeakShape = shapeThreshold(featureBlob);
                        bool lowIsotopicAffinity = isotopeThreshold(featureBlob);
                        FeatureScoreHolder currentScoreHolder = scoresTable[featureBlob];
                        if (detailedVerbose)
                        {
                            Trace.WriteLine(String.Format("        Candidate feature found at scan number {0}", featureBlob.Statistics.ScanImsRep));
                            Trace.WriteLine(String.Format("            IntensityScore: {0:F4}", currentScoreHolder.IntensityScore));
                            if (!lowIntenstity)
                            {
                                Trace.WriteLine(String.Format("            peakShapeScore: {0:F4}", currentScoreHolder.PeakShapeScore));
                            
                                if (targetComposition != null)
                                {
                                    Trace.WriteLine(String.Format("            isotopicScore:  {0:F4}", currentScoreHolder.IsotopicScore));
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
                        IDictionary<FeatureBlob, FeatureScoreHolder> qualifiedFeatures = featureBlobs.ToDictionary(feature => feature, feature => scoresTable[feature]);
                        // Assign the best feature to voltage group it belongs to, with the feature score as one of the criteria of that voltage group.
                
                        // TODO: If there are more than one apexes in the best feature. Treat them as isomers.
                
                        voltageGroup.BestFeature = ScoreUtil.SelectMostLikelyFeature(qualifiedFeatures, likelihoodFunc);
                        voltageGroup.BestFeatureScores = scoresTable[voltageGroup.BestFeature];
                    }
                    else 
                    {
                        if (detailedVerbose)
                        {
                            Trace.WriteLine(String.Format("    (All features were rejected in voltage group {0:F4} V)", voltageGroup.MeanVoltageInVolts));
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
                    informedResult = new MoleculeInformedWorkflowResult(
                        this.DatasetName,
                        targetDescription,
                        target.IonizationType,
                        AnalysisStatus.NEG,
                        analysisScores,
                        null);
                    
                    Trace.WriteLine("Analysis result");
                    Trace.WriteLine(String.Format("    Analysis Conclusion: {0}", informedResult.AnalysisStatus));
                    if (detailedVerbose)
                    {
                        Trace.WriteLine(String.Format("    Average Voltage Group Stability Score {0:F4}", informedResult.AnalysisScoresHolder.AverageVoltageGroupStabilityScore));
                        Trace.WriteLine(String.Format("    Average Best Feature Intensity Score {0:F4}",    informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.IntensityScore));
                        
                        if (targetComposition != null)
                        {
                            Trace.WriteLine(String.Format("    Average Best Feature Isotopic Score {0:F4}",     informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.IsotopicScore));
                        }

                        Trace.WriteLine(String.Format("    Average Best Feature Peak Shape Score {0:F4}", informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.PeakShapeScore));
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
                        Trace.WriteLine(String.Format("    Target presence confirmed at {0:F2} ± {1:F2} V.", voltageGroup.MeanVoltageInVolts, Math.Sqrt (voltageGroup.VarianceVoltage)));
                        Trace.WriteLine(String.Format("        Frame range: [{0}, {1}]", voltageGroup.FirstFrameNumber - 1,     voltageGroup.FirstFrameNumber   +voltageGroup.AccumulationCount - 2));
                        Trace.WriteLine(String.Format("        Drift time: {0:F4} ms (Scan# = {1})", voltageGroup.FitPoint.X * 1000,    voltageGroup.BestFeature.Statistics.ScanImsRep));
                                                               
                        Trace.WriteLine(String.Format("        Cook's distance: {0:F4}", voltageGroup.FitPoint.CooksD));
                        Trace.WriteLine(String.Format("        VoltageGroupScore: {0:F4}", voltageGroup.VoltageGroupScore));
                        Trace.WriteLine(String.Format("        IntensityScore: {0:F4}", voltageGroup.BestFeatureScores.IntensityScore));
                        if (targetComposition != null)
                        {
                            Trace.WriteLine(String.Format("        IsotopicScore: {0:F4}", voltageGroup.BestFeatureScores.IsotopicScore));
                        }
                
                        Trace.WriteLine(String.Format("        PeakShapeScore: {0:F4}", voltageGroup.BestFeatureScores.PeakShapeScore));
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
                    informedResult = new MoleculeInformedWorkflowResult(
                        this.DatasetName,
                        targetDescription,
                        target.IonizationType,
                        AnalysisStatus.NSP,
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
                AnalysisStatus finalStatus = AnalysisFilter.FilterLowR2(rSquared) ? AnalysisStatus.REJ : AnalysisStatus.POS;
                
                // Check if the last voltage group still exists as fit point
                if (finalStatus == AnalysisStatus.REJ)
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
                
                informedResult = new MoleculeInformedWorkflowResult(
                this.DatasetName,
                targetDescription,
                target.IonizationType,
                finalStatus,
                analysisScoreHolder,
                isomers);
                
                Trace.WriteLine("Analysis result");
                Trace.WriteLine(String.Format("    Analysis Conclusion: {0}", informedResult.AnalysisStatus));
                Trace.WriteLine(String.Format("    R Squared {0:F4}", informedResult.AnalysisScoresHolder.RSquared));

                if (detailedVerbose)
                {
                    Trace.WriteLine(String.Format("    Average Voltage Group Stability Score {0:F4}", informedResult.AnalysisScoresHolder.AverageVoltageGroupStabilityScore));
                    Trace.WriteLine(String.Format("    Average Candidate Target Intensity Score {0:F4}",    informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.IntensityScore));
                    
                    if (targetComposition != null)
                    {
                        Trace.WriteLine(String.Format("    Average Candidate Target Isotopic Score {0:F4}",     informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.IsotopicScore));
                    }
                    
                    Trace.WriteLine(String.Format("    Average Candidate Target Peak Shape Score {0:F4}", informedResult.AnalysisScoresHolder.AverageCandidateTargetScores.PeakShapeScore));
                }
                
                int isomerIndex = 1;
                bool onlyOneIsomer = informedResult.MatchingIsomers.Count() <= 1;
                foreach (var isomer in informedResult.MatchingIsomers)
                {
                    if (!onlyOneIsomer)
                    {
                        Trace.WriteLine(String.Format("    Isomer #[{0}]" , isomerIndex));
                    }
                
                    Trace.WriteLine(String.Format("    Last VoltageGroup Drift Time: {0:F4} ms", isomer.LastVoltageGroupDriftTimeInMs));
                    Trace.WriteLine(String.Format("    Mobility: {0:F4} cm^2/(s*V)", isomer.Mobility));
                    Trace.WriteLine(String.Format("    Cross Sectional Area: {0:F4} Å^2", isomer.CrossSectionalArea));
                    isomerIndex++;
                
                    if (!onlyOneIsomer)
                    {
                        Trace.WriteLine("");
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
                
                using (StreamWriter resultFile = File.AppendText(result))
                {
                    TextWriterTraceListener resultFileTraceListener = new TextWriterTraceListener(resultFile)
                    {
                        Name = "this.DatasetName" + "_Result",
                        TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
                    };
                
                    Trace.Listeners.Add(consoleTraceListener);
                    Trace.Listeners.Add(resultFileTraceListener);
                    Trace.AutoFlush = true;
                    Trace.WriteLine(e.Message);
                    Trace.WriteLine(e.StackTrace);
                }

                // create the error result
                AnalysisScoresHolder analysisScores;
                analysisScores.RSquared = 0;
                analysisScores.AverageCandidateTargetScores.IntensityScore = 0;
                analysisScores.AverageCandidateTargetScores.IsotopicScore = 0;
                analysisScores.AverageCandidateTargetScores.PeakShapeScore = 0;
                analysisScores.AverageVoltageGroupStabilityScore = 0;

                informedResult = new MoleculeInformedWorkflowResult(
                    this.DatasetName,
                    targetDescription,
                    target.IonizationType,
                    AnalysisStatus.ERR,
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
        /// Finalizer
        /// </summary>
        ~MoleculeInformedWorkflow()
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
                this._uimfReader.Dispose();
                this.DatasetName = null;
                this.OutputPath = null;
            }

            // free native resources if there are any.
            Trace.Listeners.Clear();
        }
    }
}
