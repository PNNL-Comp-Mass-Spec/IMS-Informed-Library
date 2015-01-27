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
    using ImsInformed.IO;
    using ImsInformed.Parameters;
    using ImsInformed.Scoring;
    using ImsInformed.Stats;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

    using Microsoft.SqlServer.Server;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// Find molecules with a known formula and know ionization methods. metabolites and pipetides alike.
    /// </summary>
    public class MoleculeInformedWorkflow : InformedWorkflow
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

        /// <summary>
        /// The run molecule informed work flow.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="MoleculeInformedWorkflowResult"/>.
        /// </returns>
        public MoleculeInformedWorkflowResult RunMoleculeInformedWorkFlow(ImsTarget target)
        {
            try
            {
                // Initialize the result object
                MoleculeInformedWorkflowResult informedResult;
                informedResult.DatasetName = this.DatasetName;
                informedResult.IonizationMethod = target.IonizationType;
                
                // ImsTarget assumes proton+ ionization. Get rid of it here.
                Composition targetComposition = MoleculeUtil.IonizationCompositionCompensation(target.Composition, target.IonizationType);
                informedResult.TargetDescriptor = (targetComposition == null) ? target.TargetMz.ToString(CultureInfo.InvariantCulture) : target.EmpiricalFormula;
                
                // Setup result file.
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
                    
                    targetComposition = MoleculeUtil.IonizationCompositionDecompensation(targetComposition, IonizationMethod.ProtonPlus);

                    // Setup target object
                    if (targetComposition != null) 
                    {
                        // Because Ion class from Informed Proteomics assumes adding a proton, that's the reason for decompensation
                        Ion targetIon = new Ion(targetComposition, 1);
                        target.TargetMz = targetIon.GetMonoIsotopicMz();
                    } 
                    
                    resultFile.WriteLine();
                    Trace.WriteLine("Dataset: " + this.DatasetName);
                    Trace.WriteLine("Ionization method: " + target.IonizationType);
                    Trace.WriteLine("Targeting Mz: " + target.TargetMz);
                        
                    // Generate Theoretical Isotopic Profile
                    List<Peak> theoreticalIsotopicProfilePeakList = null;
                    if (targetComposition != null) 
                    {
                        string empiricalFormula = (targetComposition != null) ? targetComposition.ToPlainString() : string.Empty;
                        IsotopicProfile theoreticalIsotopicProfile = _theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, 1);
                        theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
                    }
                
                    // Generate VoltageSeparatedAccumulatedXICs
                    VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(_uimfReader, target.TargetMz,     _parameters);
                        
                    // For each voltage, find 2D XIC features 
                    double totalScore = 0;
                
                    // Because we can't delete keys while iterating over a dictionary, and thus removalList
                    List<VoltageGroup> removaList = new List<VoltageGroup>();
                    foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                    {
                        // The filters below were written for 3D XICs, but they should work for 2D XICs.
                
                        // Smooth Chromatogram
                        IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(accumulatedXiCs[voltageGroup].IntensityPoints);
                        _smoother.Smooth(ref pointList);
                        
                        // Peak Find Chromatogram
                        IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);

                        // 1st round filtering: reject small feature peaks. Fast filtering.
                        featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, this.Parameters.FeatureFilterLevel);
                
                        // Trace.WriteLine(" .After Filtering: " + featureBlobs.Count());
                        // Find the global intensity MAX, used for noise rejection
                        double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, _uimfReader);
                
                        // 2st round filtering: filter out non real peaks and score using isotopic score. 
                        FeatureBlob bestFeature = null;
                        double bestSecondRoundFilterScore = 0;

                        // Check each XIC Peak found
                        foreach (var featureBlob in featureBlobs)
                        {
                            // Evalute feature scores.
                            double intensityScore = FeatureScore.IntensityScore(this, featureBlob, voltageGroup);
                            
                            double isotopicScore = 0;
                            if (targetComposition != null)
                            {
                                 isotopicScore = FeatureScore.IsotopicProfileScore(this, target, featureBlob.Statistics, theoreticalIsotopicProfilePeakList, this._uimfReader, voltageGroup);
                            }
                            // double peakShapeScore = FeatureScore.PeakShapeScore(featureBlobs);

                            double secondRoundFilterScore = intensityScore;
                            //double secondRoundFilterScore = isotopicScore;
                            if (secondRoundFilterScore > bestSecondRoundFilterScore)
                            {
                                bestFeature = featureBlob;
                                bestSecondRoundFilterScore = secondRoundFilterScore;
                            }
                        }

                        // Assign the best feature to voltage group it belongs to, with the feature score as one of the criteria of that voltage group.
                        voltageGroup.BestFeature = bestFeature;
                
                        // Rate the feature's VoltageGroupScore score. VoltageGroupScore score measures how likely the voltage group contains and detected the target ion.
                        voltageGroup.VoltageGroupScore = FeatureScore.RealPeakScore(this, bestFeature, globalMaxIntensity);
                
                        voltageGroup.BestScore = bestSecondRoundFilterScore;
                        totalScore += bestSecondRoundFilterScore;
                        if (voltageGroup.BestFeature == null || voltageGroup.VoltageGroupScore < this.Parameters.ConfidenceThreshold) 
                        {
                            Console.WriteLine("Nothing is found in voltage group {0:F2} V", voltageGroup.MeanVoltageInVolts);
                            removaList.Add(voltageGroup);
                        } 
                    }
                
                    double averageScore = totalScore / accumulatedXiCs.Keys.Count;
                    double threshold = averageScore / 10;
                    foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                    {
                        if (voltageGroup.BestScore < threshold)
                        {
                            removaList.Add(voltageGroup);
                        }
                    }
                        
                    // Remove stuff
                    foreach (VoltageGroup voltageGroup in removaList)
                    {
                        accumulatedXiCs.Remove(voltageGroup);
                    }
                
                    // Reject voltage groups without ion presence.
                    if (accumulatedXiCs.Keys.Count < 1)
                    {
                        Trace.WriteLine("Target Ion not found in this UIMF file");
                        informedResult.AnalysisStatus = AnalysisStatus.NEG;
                        informedResult.Mobility = 0;
                        informedResult.CrossSectionalArea = 0;
                        informedResult.RSquared = 0;
                        return informedResult;
                    }
                
                    // Calculate the fit line 
                    HashSet<ContinuousXYPoint> fitPoints = new HashSet<ContinuousXYPoint>();
                    foreach (VoltageGroup group in accumulatedXiCs.Keys)
                    {
                        // convert drift time to SI unit seconds
                        double x = group.BestFeature.Statistics.ScanImsRep * group.AverageTofWidthInSeconds;
                    
                        // P/(T*V) value in pascal per (volts * kelvin)
                        double y = group.MeanPressureNondimensionalized / group.MeanVoltageInVolts
                                   / group.MeanTemperatureNondimensionalized;
                         
                        ContinuousXYPoint point = new ContinuousXYPoint(x, y);
                        fitPoints.Add(point);
                        group.FitPoint = point;
                    }
                
                    double driftTubeLength = FakeUIMFReader.DriftTubeLengthInCentimeters;
                    FitLine line = new FitLine(fitPoints, 3);            
                
                    // Mark outliers and compute the fitline without using the outliers.
                    HashSet<ContinuousXYPoint> newPoints = new HashSet<ContinuousXYPoint>();
                    foreach (ContinuousXYPoint point in fitPoints)
                    {
                        if (!point.IsOutlier)
                        {
                            newPoints.Add(point);
                        }
                    }
                
                    // If not enough points
                    bool sufficientPoints = newPoints.Count >= 3;
                    if (!sufficientPoints)
                    {
                        Trace.WriteLine("Not enough points are qualified for perform linear fit. Abort identification.");
                        informedResult.AnalysisStatus = AnalysisStatus.NSP;
                        informedResult.Mobility = 0;
                        informedResult.CrossSectionalArea = 0;
                        informedResult.RSquared = 0;
                        return informedResult;
                    }
                    else 
                    {
                        line.LeastSquaresFitLinear(newPoints);
                
                        // Export the fit line into QC oxyplot drawings
                        string outputPath = this.OutputPath + this.DatasetName + "_" + target.IonizationType + "_QA.png";
                        ImsInformedPlotter.MobilityFitLine2PNG(outputPath, line);
                        Console.WriteLine("Writes QC plot of fitline to " + outputPath);
                        Trace.WriteLine(string.Empty);
                
                        double rSquared = line.RSquared;
                
                        // Compute mobility and cross section area
                        double mobility = driftTubeLength * driftTubeLength / (1 / line.Slope);
                        Composition bufferGas = new Composition(0, 0, 2, 0, 0);
                        double reducedMass = MoleculeUtil.ComputeReducedMass(target.TargetMz, bufferGas);
                        
                        // Find the average temperature across from various voltage groups.
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
                        informedResult.AnalysisStatus = AnalysisStatus.POS;
                        informedResult.Mobility = mobility;
                        informedResult.CrossSectionalArea = crossSection;
                        informedResult.RSquared = rSquared;
                
                        // Printout results
                        foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                        {
                            Trace.WriteLine(String.Format("Target presence found:\nVariance: {0:F2}.", voltageGroup.VarianceVoltage));
                            Trace.WriteLine(String.Format("Mean voltage {0:F2} V", voltageGroup.MeanVoltageInVolts));
                            Trace.WriteLine(String.Format("Frame range: [{0}, {1}]", voltageGroup.FirstFrameNumber - 1,     voltageGroup.FirstFrameNumber+voltageGroup.AccumulationCount - 2));
                            Trace.WriteLine(String.Format("score: {0:F2}",  voltageGroup.BestScore));
                            Trace.WriteLine(String.Format("Scan number: {0}", voltageGroup.BestFeature.Statistics.ScanImsRep));
                            Trace.WriteLine(String.Format("ImsTime: {0:F2} ms", voltageGroup.FitPoint.x * 1000));
                            // FOR COMPARISON WITH MATT"S RESULT, UNCOMMENT IF YOU SEE IT
                            informedResult.Mobility = voltageGroup.FitPoint.x * 1000;
                            // Normalize the drift time to be displayed.
                            informedResult.Mobility = MoleculeUtil.NormalizeDriftTime(informedResult.Mobility, voltageGroup);
                            Trace.WriteLine(String.Format("Cook's distance: {0:F2}", voltageGroup.FitPoint.CooksD));
                            Trace.WriteLine(String.Format("Confidence: {0:F2}", voltageGroup.VoltageGroupScore));
                            Trace.WriteLine(string.Empty);
                        }
                        Trace.WriteLine(String.Format("R Squared {0:F4}", informedResult.RSquared));
                        Trace.WriteLine(String.Format("Mobility: {0:F2} cm^2/(s*V)", informedResult.Mobility));
                        Trace.WriteLine(String.Format("Cross Sectional Area: {0:F2} Å^2", informedResult.CrossSectionalArea));

                        return informedResult;
                    }
                }
            }
            catch (Exception)
            {
                // create the error result
                MoleculeInformedWorkflowResult informedResult;
                informedResult.DatasetName = this.DatasetName;
                informedResult.TargetDescriptor = null;
                informedResult.IonizationMethod = target.IonizationType;
                informedResult.AnalysisStatus = AnalysisStatus.ERR;
                informedResult.Mobility = 0;
                informedResult.CrossSectionalArea = 0;
                informedResult.RSquared = 0;
                return informedResult;
            }
        }
    }
}
