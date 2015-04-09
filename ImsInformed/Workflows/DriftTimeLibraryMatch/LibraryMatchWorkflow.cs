// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LibraryMatchWorkflow.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the LibraryMatchWorkflow type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Workflows.DriftTimeLibraryMatch
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using DeconTools.Backend.Core;
    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Filters;
    using ImsInformed.Scoring;
    using ImsInformed.Targets;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Composition;

    using MagnitudeConcavityPeakFinder;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using PNNLOmics.Data.Features;

    using UIMFLibrary;

    public class LibraryMatchWorkflow
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
        /// Initializes a new instance of the <see cref="LibraryMatchWorkflow"/> class.
        /// </summary>
        /// <param name="uimfFileLocation">
        /// The UIMF file location.
        /// </param>
        /// <param name="outputDirectory">
        /// The output directory.
        /// </param>
        /// <param name="resultFileName">
        /// The result file name.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public LibraryMatchWorkflow(string uimfFileLocation, string outputDirectory, string resultFileName, LibraryMatchParameters parameters)
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
        /// The smoother.
        /// </summary>
        protected readonly SavitzkyGolaySmoother smoother;

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public LibraryMatchParameters Parameters { get; set; }

        /// <summary>
        /// Gets or sets the dataset name.
        /// </summary>
        public string DatasetName { get; private set; }

        /// <summary>
        /// The file writer.
        /// </summary>
        private readonly StreamWriter resultFileWriter; 
        

        /// <summary>
        /// Gets the result path.
        /// </summary>
        public string ResultFileName { get; private set; }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string OutputPath { get; private set; }

        /// <summary>
        /// The run library match workflow.
        /// </summary>
        /// <param name="targetList">
        /// The target list.
        /// </param>
        /// <returns>
        /// The <see cref="IDictionary"/>.
        /// </returns>
        public IDictionary<DriftTimeTarget, LibraryMatchResult> RunLibraryMatchWorkflow(IEnumerable<DriftTimeTarget> targetList)
        {
            IDictionary<DriftTimeTarget, LibraryMatchResult> targetResultMap = new Dictionary<DriftTimeTarget, LibraryMatchResult>();
            foreach (DriftTimeTarget target in targetList)
            {
                Console.Write("    Target: " + target.EmpiricalFormula);
                Console.WriteLine(" (m/z = {0})", target.MonoisotopicMass);
                Console.WriteLine(" (Drift time = {0})", target.DriftTime);

                LibraryMatchResult result = this.RunCrossSectionWorkFlow(target);
                targetResultMap.Add(target, result);
            }

            return targetResultMap;
        }

        /// <summary>
        /// The run cross section work flow.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="LibraryMatchResult"/>.
        /// </returns>
        public LibraryMatchResult RunCrossSectionWorkFlow(DriftTimeTarget target)
        {
            string targetDescription = target.TargetDescriptor;

            try
            {
                Trace.WriteLine("Dataset: " + this.DatasetName);
                Trace.WriteLine("Ionization method: " + target.Adduct);
                Trace.WriteLine("Target Mz: " + target.MassWithAdduct);
                Trace.WriteLine("Target Drift time: " + target.DriftTime);
                Trace.WriteLine(string.Empty);

                // Generate Theoretical Isotopic Profile
                List<Peak> theoreticalIsotopicProfilePeakList = null;
                string empiricalFormula = target.CompositionWithAdduct.ToPlainString();
                ITheorFeatureGenerator featureGenerator = new JoshTheorFeatureGenerator();
                IsotopicProfile theoreticalIsotopicProfile = featureGenerator.GenerateTheorProfile(empiricalFormula, 1);
                theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
                
                // Voltage grouping
                VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(this.uimfReader, target.MassWithAdduct, this.Parameters.FeatureFilterLevel);

                foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
                {    
                    // Verify the temperature, pressure and drift tube voltage of the voltage group

                    double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, this.uimfReader);
                
                    // Find peaks using multidimensional peak finder.
                    List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;
                    List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, this.smoother, this.Parameters.FeatureFilterLevel);
                    List<StandardImsPeak> standardPeaks = featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, this.uimfReader, voltageGroup, target.MassWithAdduct, this.Parameters.MassToleranceInPpm)).ToList();
                
                    // Score features
                    IDictionary<StandardImsPeak, FeatureScoreHolder> scoresTable = new Dictionary<StandardImsPeak, FeatureScoreHolder>();
                    Trace.WriteLine(string.Format("    Voltage Group: {0:F4} V, [{1}-{2}]", voltageGroup.MeanVoltageInVolts, voltageGroup.FirstFrameNumber, voltageGroup.LastFrameNumber));

                    foreach (StandardImsPeak peak in standardPeaks)
                    {   
                        FeatureScoreHolder currentScoreHolder;
                        currentScoreHolder.IntensityScore = FeatureScores.IntensityScore(peak, voltageGroup, globalMaxIntensity);
                        
                        currentScoreHolder.PeakShapeScore = FeatureScores.PeakShapeScore(peak, this.uimfReader, this.Parameters.MassToleranceInPpm, this.Parameters.DriftTimeToleranceInMs, voltageGroup, globalMaxIntensity, this.NumberOfScans);
                
                        currentScoreHolder.IsotopicScore = FeatureScores.IsotopicProfileScore(peak, this.uimfReader, this.Parameters.MassToleranceInPpm, this.Parameters.DriftTimeToleranceInMs, target, theoreticalIsotopicProfilePeakList, voltageGroup,IsotopicScoreMethod.Angle, globalMaxIntensity, this.NumberOfScans);
                
                        scoresTable.Add(peak, currentScoreHolder);
                    }
                
                    // filter out features with Ims scans at 1% left or right.
                    Predicate<StandardImsPeak> scanPredicate = blob => FeatureFilters.FilterExtremeDriftTime(blob, (int)this.NumberOfScans);
                    Predicate<StandardImsPeak> shapeThreshold = blob => FeatureFilters.FilterBadPeakShape(blob, scoresTable[blob].PeakShapeScore, this.Parameters.PeakShapeThreshold);
                    Predicate<StandardImsPeak> isotopeThreshold = blob => FeatureFilters.FilterBadIsotopicProfile(blob, scoresTable[blob].IsotopicScore, this.Parameters.IsotopicThreshold);
                
                    // Print out candidate features that pass the intensity threshold.
                    foreach (StandardImsPeak peak in standardPeaks)
                    {  
                        bool badScanRange = scanPredicate(peak);
                        bool badPeakShape = shapeThreshold(peak);
                        bool lowIsotopicAffinity = isotopeThreshold(peak);
                        FeatureScoreHolder currentScoreHolder = scoresTable[peak];
                        Trace.WriteLine(string.Format("        Candidate feature found at scan number {0}", peak.HighestPeakApex.DriftTimeCenterInScanNumber));
                        Trace.WriteLine(string.Format("            IntensityScore: {0:F4}", currentScoreHolder.IntensityScore));
                        Trace.WriteLine(string.Format("            peakShapeScore: {0:F4}", currentScoreHolder.PeakShapeScore));
                        Trace.WriteLine(string.Format("            isotopicScore:  {0:F4}", currentScoreHolder.IsotopicScore));
                
                        string rejectionReason = badScanRange ? "        [Bad scan range] " : "        ";
                        rejectionReason += badPeakShape ? "[Bad Peak Shape] " : string.Empty;
                        rejectionReason += lowIsotopicAffinity ? "[Different Isotopic Profile] " : string.Empty;

                        if (badScanRange || lowIsotopicAffinity || badPeakShape)
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

                return new LibraryMatchResult();
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
                return new LibraryMatchResult();
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
        /// Finalizes an instance of the <see cref="LibraryMatchWorkflow"/> class. 
        /// Finalizer
        /// </summary>
        ~LibraryMatchWorkflow()
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
