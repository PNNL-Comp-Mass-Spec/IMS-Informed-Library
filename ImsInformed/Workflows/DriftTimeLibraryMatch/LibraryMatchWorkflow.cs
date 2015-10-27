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
namespace ImsInformed.Workflows.DriftTimeLibraryMatch
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows.Media.TextFormatting;

    using DeconTools.Backend.Core;
    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Filters;
    using ImsInformed.Scoring;
    using ImsInformed.Targets;
    using ImsInformed.Util;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    public class LibraryMatchWorkflow
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
        /// <param name="targetList">dD
        /// The Target list.
        /// </param>
        /// <returns>
        /// The <see cref="IDictionary"/>.
        /// </returns>
        public IDictionary<DriftTimeTarget, LibraryMatchResult> RunLibraryMatchWorkflow(IEnumerable<DriftTimeTarget> targetList)
        {
            Trace.WriteLine(string.Format("Dataset: " + this.DatasetName));

            IDictionary<DriftTimeTarget, LibraryMatchResult> targetResultMap = new Dictionary<DriftTimeTarget, LibraryMatchResult>();
            foreach (DriftTimeTarget target in targetList)
            {
                LibraryMatchResult result = this.RunLibrayMatchWorkflow(target);
                targetResultMap.Add(target, result);
            }

            return targetResultMap;
        }

        /// <summary>
        /// The run libray match workflow.
        /// </summary>
        /// <param name="target">
        /// The Target.
        /// </param>
        /// <returns>
        /// The <see cref="LibraryMatchResult"/>.
        /// </returns>
        private LibraryMatchResult RunLibrayMatchWorkflow(DriftTimeTarget target)
        {
            Trace.WriteLine("    Target: " + target.CorrespondingChemical);
            Trace.WriteLine("    Target Info: " + target.TargetDescriptor);

            // Generate Theoretical Isotopic Profile
            List<Peak> theoreticalIsotopicProfilePeakList = null;
            string empiricalFormula = target.CompositionWithAdduct.ToPlainString();
            ITheorFeatureGenerator featureGenerator = new JoshTheorFeatureGenerator();
            IsotopicProfile theoreticalIsotopicProfile = featureGenerator.GenerateTheorProfile(empiricalFormula, 1);
            theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();

            // Voltage grouping
            VoltageSeparatedAccumulatedXiCs accumulatedXiCs = new VoltageSeparatedAccumulatedXiCs(this.uimfReader, target.MassWithAdduct, this.Parameters.InitialSearchMassToleranceInPpm, target.NormalizedDriftTimeInMs, this.Parameters.DriftTimeToleranceInMs);

            foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
            {
                // TODO Verify the temperature, pressure and drift tube voltage of the voltage group
                // Because we don't record TVP info in the AMT library. we can't verify it yet, so stick with last voltage group.
                if (IMSUtil.IsLastVoltageGroup(voltageGroup, this.NumberOfFrames))
                {
                    double globalMaxIntensity = IMSUtil.MaxIntensityAfterFrameAccumulation(voltageGroup, this.uimfReader);

                    Trace.WriteLine(string.Format("    Temperature/Pressure/Voltage Adjusted Drift time: {0:F4} ms", IMSUtil.DeNormalizeDriftTime(target.NormalizedDriftTimeInMs, voltageGroup)));
            
                    // Find peaks using multidimensional peak finder.
                    List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;
                    List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, this.smoother, this.Parameters.FeatureFilterLevel);
                    List<StandardImsPeak> standardPeaks = featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, this.uimfReader, voltageGroup, target.MassWithAdduct, this.Parameters.InitialSearchMassToleranceInPpm)).ToList();
            
                    // Score features
                    IDictionary<StandardImsPeak, PeakScores> scoresTable = new Dictionary<StandardImsPeak, PeakScores>();
                    Trace.WriteLine(string.Format("    Voltage Group: {0:F4} V, [{1}-{2}]", voltageGroup.MeanVoltageInVolts, voltageGroup.FirstFirstFrameNumber, voltageGroup.LastFrameNumber));

                    foreach (StandardImsPeak peak in standardPeaks)
                    {   
                        PeakScores currentStatistics = FeatureScoreUtilities.ScoreFeature(
                            peak,
                            globalMaxIntensity,
                            this.uimfReader,
                            this.Parameters.InitialSearchMassToleranceInPpm,
                            this.Parameters.DriftTimeToleranceInMs,
                            voltageGroup,
                            this.NumberOfScans,
                            target,
                            IsotopicScoreMethod.Angle, 
                            theoreticalIsotopicProfilePeakList);
                        scoresTable.Add(peak, currentStatistics);
                    }
            
                    // filter out features with Ims scans at 1% left or right.
                    Predicate<StandardImsPeak> scanPredicate = blob => FeatureFilters.FilterExtremeDriftTime(blob, (int)this.NumberOfScans);
                    Predicate<StandardImsPeak> shapeThreshold = blob => FeatureFilters.FilterBadPeakShape(blob, scoresTable[blob].PeakShapeScore, this.Parameters.PeakShapeThreshold);
                    Predicate<StandardImsPeak> isotopeThreshold = blob => FeatureFilters.FilterBadIsotopicProfile(blob, scoresTable[blob].IsotopicScore, this.Parameters.IsotopicThreshold);
                    Predicate<StandardImsPeak> massDistanceThreshold = blob => FeatureFilters.FilterHighMzDistance(blob, target, this.Parameters.MatchingMassToleranceInPpm);
            
                    // Print out candidate features that pass the intensity threshold.
                    foreach (StandardImsPeak peak in standardPeaks)
                    {  
                        DriftTimeFeatureDistance distance = new DriftTimeFeatureDistance(target, peak, voltageGroup);

                        bool badScanRange = scanPredicate(peak);
                        bool badPeakShape = shapeThreshold(peak);
                        bool lowIsotopicAffinity = isotopeThreshold(peak);
                        bool lowMzAffinity = massDistanceThreshold(peak);

                        PeakScores currentStatistics = scoresTable[peak];
                        Trace.WriteLine(string.Empty);
                        Trace.WriteLine(string.Format("        Candidate feature found at drift time {0:F2} ms (scan number {1})",
                            peak.HighestPeakApex.DriftTimeCenterInMs,
                            peak.HighestPeakApex.DriftTimeCenterInScanNumber));
                        Trace.WriteLine(
                            string.Format(
                                "            M/Z: {0:F2} Dalton", peak.HighestPeakApex.MzCenterInDalton));
                        Trace.WriteLine(
                            string.Format(
                                "            Drift time: {0:F2} ms (scan number {1})",
                                peak.HighestPeakApex.DriftTimeCenterInMs,
                                peak.HighestPeakApex.DriftTimeCenterInScanNumber));
                        Trace.WriteLine(
                            string.Format("            MzInDalton difference: {0:F2} ppm", distance.MassDifferenceInPpm));
                        
                        Trace.WriteLine(
                            string.Format("            Drift time difference: {0:F4} ms", distance.DriftTimeDifferenceInMs));

                        Trace.WriteLine(string.Format("            Intensity Score: {0:F4}", currentStatistics.IntensityScore));
                        Trace.WriteLine(string.Format("            Peak Shape Score: {0:F4}", currentStatistics.PeakShapeScore));
                        Trace.WriteLine(string.Format("            Isotopic Score:  {0:F4}", currentStatistics.IsotopicScore));
                        Trace.WriteLine(string.Format("            AveragedPeakIntensities:  {0:F4}", peak.SummedIntensities));
            
                        string rejectionReason = badScanRange ? "        [Bad scan range] " : "        ";
                        rejectionReason += badPeakShape ? "[Bad Peak Shape] " : string.Empty;
                        rejectionReason += lowMzAffinity ? "[Inaccurate Mass] " : string.Empty;
                        rejectionReason += lowIsotopicAffinity ? "[Different Isotopic Profile] " : string.Empty;

                        if (badScanRange || lowIsotopicAffinity || badPeakShape)
                        {
                            Trace.WriteLine(rejectionReason);
                        }
                        else
                        {
                            Trace.WriteLine("        [Pass]");
                        }
                    }

                    standardPeaks.RemoveAll(scanPredicate);
            
                    standardPeaks.RemoveAll(massDistanceThreshold);

                    standardPeaks.RemoveAll(shapeThreshold);

                    standardPeaks.RemoveAll(isotopeThreshold);

                    if (standardPeaks.Count == 0)
                    {
                        Trace.WriteLine(string.Format("    [No Match]"));
                        Trace.WriteLine(string.Empty);
                        return new LibraryMatchResult(null, AnalysisStatus.Negative, null);
                    }

                    StandardImsPeak closestPeak = standardPeaks.First();
                    DriftTimeFeatureDistance shortestDistance = new DriftTimeFeatureDistance(target, standardPeaks.First(), voltageGroup);
                    foreach (var peak in standardPeaks)
                    {
                        DriftTimeFeatureDistance distance = new DriftTimeFeatureDistance(target, peak, voltageGroup);
                        if (distance.CompareTo(shortestDistance) < 0)
                        {
                            closestPeak = peak;
                        }
                    }

                    Trace.WriteLine(string.Format("    [Match]"));
                    Trace.WriteLine(string.Empty);
                    return new LibraryMatchResult(closestPeak, AnalysisStatus.Positive, shortestDistance);
                }
            }

            throw new Exception("No voltage groups in the Dataset match temerature, pressure, or drift tube voltage setting from the library. Matching failed.");
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
