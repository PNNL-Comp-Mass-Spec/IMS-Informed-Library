using System;
using System.Collections.Generic;
using System.Linq;
using DeconTools.Backend.Core;
using ImsInformed.Domain;
using ImsInformed.Parameters;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using MultiDimensionalPeakFinding.PeakDetection;

namespace ImsInformed.Util
{
    using System.Dynamic;
    using System.IO;

    using ImsInformed.IO;
    using ImsInformed.Stats;

    // Find molecules with a known formula and know ionization methods. metabolites and pipetides alike.
    public class MoleculeInformedWorkflow : InformedWorkflow
    {
        public MoleculeWorkflowParameters Parameters { get; set; }
        
        public string OutputPath { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoleculeInformedWorkflow"/> class.
        /// </summary>
        /// <param name="uimfFileLocation">
        /// The uimf file location.
        /// </param>
        /// <param name="outputDirectory">
        /// The output directory.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public MoleculeInformedWorkflow(string uimfFileLocation, string outputDirectory, MoleculeWorkflowParameters parameters) : base(uimfFileLocation, parameters)
		{
            this.Parameters = parameters;
            
            if (outputDirectory == "")
            {
                outputDirectory = Directory.GetCurrentDirectory();
            }
            if (Directory.Exists(outputDirectory))
            {
                this.OutputPath = outputDirectory;
            }
            else 
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception e)
                {
                    
                    Console.WriteLine("Failed to create directory.");
                    throw;
                }
            }
		}

        // return nth isotope of the target, return null if target does not have composition data
        // TODO verify if correct, i think this is wrong
        public static Ion TargetIon(ImsTarget target, int n, int chargeState)
        {
            return new Ion(target.Composition, chargeState);
        }

        public double ScoreFeatureUsingIsotopicProfile(FeatureBlob featureBlob, ImsTarget target, int chargeState, FeatureBlobStatistics statistics)
        {
            // No need to move on if the isotopic profile is not found
            //if (observedIsotopicProfile == null || observedIsotopicProfile.MonoIsotopicMass < 1)
            //{
            //    result.FailureReason = FailureReason.IsotopicProfileNotFound;
            //    continue;
            //}

            // Find Isotopic Profile
                // List<Peak> massSpectrumPeaks;
                //IsotopicProfile observedIsotopicProfile = _msFeatureFinder.IterativelyFindMSFeature(massSpectrum, theoreticalIsotopicProfile, out massSpectrumPeaks);

            int unsaturatedIsotope = 0;

            if (target.Composition == null)
            {
                throw new InvalidOperationException("Cannot score feature using isotopic profile for Ims target without Composition provided.");
            }
            FeatureBlob isotopeFeature = null;
            
            // Bad Feature, so get out
            if (statistics == null)
            {
                return 0;
            }
            
            // Find an unsaturated peak in the isotopic profile
            for (int i = 1; i < 10; i++)
            {
                // TODO: Verify that there are no peaks at isotope #s 0.5 and 1.5?? (If we filter on drift time, this shouldn't actually be necessary)
                if (!statistics.IsSaturated) break;

                double isotopeTargetMz = TargetIon(target, i, chargeState).GetIsotopeMz(i);

                // Find XIC Features
                IEnumerable<FeatureBlob> newFeatureBlobs = FindFeatures(isotopeTargetMz, statistics.ScanLcMin - 20, statistics.ScanLcMax + 20);

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
                    if(newStatistics.ScanImsRep <= statistics.ScanImsMax && newStatistics.ScanImsRep >= statistics.ScanImsMin && newStatistics.ScanLcRep <= statistics.ScanLcMax && newStatistics.ScanLcRep >= statistics.ScanLcMin)
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
            return 0;
        }

        public double ScoreFeatureMzOnly(FeatureBlob featureBlob)
        {
            // Sort features by relative intensity
            FeatureBlobStatistics statistics = featureBlob.CalculateStatistics();
            int scanImsRep = statistics.ScanImsRep;
            if ((scanImsRep < 5 || scanImsRep > _numScans - 5))
            {
                return 0;
            }
            return statistics.SumIntensities;
        }

        // Find the feature with highest matching score for the target for one voltage group
        public FeatureBlob FeaturesSelection(IEnumerable<FeatureBlob> features, ImsTarget target, int chargeState, out double score)
        {
            FeatureBlob bestBlob = null;
            double maxScore = 0;
            // Check each XIC Peak found
            foreach (var featureBlob in features)
            {
                // Find the feature with the highest intensity.
                score = this.ScoreFeatureMzOnly(featureBlob);
                if (score > maxScore)
                {
                    bestBlob = featureBlob;
                    maxScore = score;
                }
            }
            score = maxScore;
            return bestBlob;
        }

        public bool RunMoleculeInformedWorkFlow(ImsTarget target)
        {
            // ImsTarget assumes proton+ ionization. Get rid of it here.
            Composition targetComposition = MoleculeUtil.IonizationCompositionDecompensation(target.Composition, IonizationMethod.ProtonPlus);
            targetComposition = MoleculeUtil.IonizationCompositionCompensation(targetComposition, target.IonizationType);
            
            double targetMass = (targetComposition != null) ? targetComposition.Mass : target.TargetMz;
            string empiricalFormula = (targetComposition != null) ? targetComposition.ToPlainString() : "";
          
            // Setup result object
            ImsTargetResult result = new ImsTargetResult
            {
                FailureReason = FailureReason.None
            };

            // Setup target object
            if (targetComposition != null) 
            {
                // Because ion assumes adding a proton, that's the reason for decompensation
                Ion targetIon = new Ion(targetComposition, 1);
				target.TargetMz = targetIon.GetMonoIsotopicMz();
            } 
				
            Console.WriteLine("\n\rTargeting Mz: " + target.TargetMz);
                
			// Generate Theoretical Isotopic Profile
			IsotopicProfile theoreticalIsotopicProfile = _theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, 1);
			List<Peak> theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();

            // Generate VoltageSeparatedAccumulatedXICs
            VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(_uimfReader, target.TargetMz, _parameters);
                
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
                //Console.Write("Feature Blobs: " + featureBlobs.Count());
                    
                // Filter away small XIC peaks
                featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, this.Parameters.FeatureFilterLevel);
                //Console.WriteLine(" .After Filtering: " + featureBlobs.Count());

                double score = 0;;

                // Find the global intensity MAX, used for noise rejection
                double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, _uimfReader);

                // select best feature
                FeatureBlob bestFeature = this.FeaturesSelection(featureBlobs, target, 1, out score);

                // Rate the feature's confidence score. Confidence score measures how likely the feature is an ion instead of noise.
                voltageGroup.BestFeature = bestFeature;
                voltageGroup.ConfidenceScore = MoleculeUtil.NoiseClassifier(bestFeature, globalMaxIntensity);

                voltageGroup.BestScore = score;
                totalScore += score;
                if (voltageGroup.BestFeature == null || voltageGroup.ConfidenceScore < this.Parameters.ConfidenceThreshold) 
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

            // Reject voltage groups without ion presense.
            if (accumulatedXiCs.Keys.Count < 1)
            {
                Console.WriteLine("Target Ion not found in this UIMF file");
                return false;
            }

            // Calculate the fit line 
            HashSet<ContinuousXYPoint> fitPoints = new HashSet<ContinuousXYPoint>();
            foreach (VoltageGroup group in accumulatedXiCs.Keys)
            {
                // convert drift time to SI unit seconds
                double x = group.BestFeature.Statistics.ScanImsRep * FakeUIMFReader.AverageScanPeriodInMicroSeconds / 1000000;
            
                // P/(T*V) value in pascal per (volts * kelvin)
                double y = group.MeanPressureInPascal / group.MeanVoltageInVolts / group.MeanTemperatureInKelvin; 
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
                    newPoints.Add(point);
            }
            line.LeastSquaresFitLinear(newPoints);

            // Export the fit line into QC oxyplot drawings
            string outputPath = "QC.png" ; //DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")+ 
            ImsInformedPlotter.MobilityFitLine2PNG(outputPath, line);
            Console.WriteLine("Writes QC plot of fitline to " + outputPath);

            double mobility = driftTubeLength * driftTubeLength / (1/line.Slope);
            double crossSection = 0;

            // mobility and cross section area
            // Printout results
            foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
            {
                Console.WriteLine("Target presence found:\nVariance: {0:F2}.", voltageGroup.VarianceVoltage);
                Console.WriteLine("Mean Voltrage {0:F2} V", voltageGroup.MeanVoltageInVolts);
                Console.WriteLine("score: {0:F2}",  voltageGroup.BestScore);
                Console.WriteLine("mobilityScan: {0}", voltageGroup.BestFeature.Statistics.ScanImsRep);
                Console.WriteLine("ImsTime: {0:F2} ms", voltageGroup.FitPoint.x * 1000);
                Console.WriteLine("Cook's distance: {0:F2}", voltageGroup.FitPoint.CooksD);
                Console.WriteLine("Confidence: {0:F2}", voltageGroup.ConfidenceScore);
                Console.WriteLine();
            }
            Console.WriteLine("Mobility: {0:F2} cm^2/(s*V)", mobility);
            Console.WriteLine("Cross Sectional Area: " + crossSection);
            return false;
		}
    }
}
