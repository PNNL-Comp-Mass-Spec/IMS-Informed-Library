namespace ImsInformed.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DeconTools.Backend.Core;
    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Scoring;
    using ImsInformed.Statistics;
    using ImsInformed.Targets;
    using ImsInformed.Util;
    using ImsInformed.Workflows.CrossSectionExtraction;

    using InformedProteomics.Backend.Data.Composition;

    using MathNet.Numerics.Distributions;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using NUnit.Framework;

    using UIMFLibrary;

    // Test for internal mechanisms like scroing methods or isotopic profile extractors
    internal class InternalTests
    {
        /// <summary>
        /// The test molecule util.
        /// </summary>
        [Test]
        public void TestMoleculeUtil()
        {
            List<string> testCases = new List<string>();
            testCases.Add("O2S[C6H3(CH3)OH]2");
            testCases.Add("C13H13ClN4O2S"); //case test
            testCases.Add("c18H24n2O1P2s2"); //case test
            testCases.Add("(CF3)2C(C6H4OH)2");
            testCases.Add("  CH3COOH ");
            
            testCases.Add("C16D10");
            testCases.Add("FeS");
            testCases.Add("C18H24N2O10P2S2");
            
            testCases.Add("Jian22TNT250"); //Random string
            testCases.Add("(NH4)2SO4");
            

            testCases.Add("()[]<>{}");
            testCases.Add("(<");
            testCases.Add("]}");
            testCases.Add("()<");
            testCases.Add("(][)");
            testCases.Add("{(X)[XY]}");


            foreach (string testCase in testCases)
            {
                Console.WriteLine(testCase);
                try
                {
                    Composition compo = MoleculeUtil.ReadEmpiricalFormula(testCase);
                    Console.WriteLine("{0}.\r\n", compo);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("\r\n");
                }
            }
        }

        /// <summary>
        /// The test saturation.
        /// </summary>
        [Test]
        public void TestSaturation()
        {
            const string uimfLocation = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\peptide\DR_40ms_100_23Apr14_0002.UIMF";
            //const double mz = 432.90; // Angiotensin +3
            //const double mz = 712.20; // Melittin +4
            //const double mz = 569.96; // Melittin +5
            //const double mz = 466.54; // Tetraoctylammonium +1
            const double ppmTolerance = 50;

            List<double> mzList = new List<double>{432.9, 712.2, 569.96, 466.54};
            List<string> peptideList = new List<string> { "DRVYIHPFHL", "GIGAVLKVLTTGLPALISWIKRKRQQ", "Tetraoctylammonium", "Tetraoctylammonium Bromide" };

            SaturationDetector saturationDetector = new SaturationDetector(uimfLocation);

            //foreach (var mz in mzList)
            //{
            //    Console.WriteLine(mz);
            //    saturationDetector.GetIntensity(mz, ppmTolerance);	
            //    Console.WriteLine("*******************************************");
            //    Console.WriteLine("*******************************************");
            //}

            foreach (string peptide in peptideList)
            {
                saturationDetector.GetIntensity(peptide, ppmTolerance);
            }
        }

        [Test][STAThread]
        public void NormalityTestAlgorithmTest()
        {
            int sampleSize = 100;
            int numberOfMonteCarloTests = 5;
            double result = 0;
            NormalityTest.NormalityTestFunc normalityTestFunc = NormalityTest.JaqueBeraTest;
            
            // normal distribution
            Console.WriteLine("normal var");
            for (int i = 0; i < numberOfMonteCarloTests; i++)
            {
                var distribution = new Normal(50, 1);
                distribution.RandomSource = new Random(DateTime.Now.Millisecond * i);
                var normalSamples = distribution.Samples().Take(sampleSize);
                

                double[] reallIsNormalSamples = normalSamples.ToArray();

                result = normalityTestFunc(reallIsNormalSamples);
                Console.WriteLine("reallIsNormalSamples_" + i + ": " + result);
            }

            Console.WriteLine();

            // random distribution.
            Console.WriteLine("rand var");
            Random rnd = new Random();
            double[] randomArray = new double[sampleSize];
            for (int i = 0; i < numberOfMonteCarloTests; i++)
            {
                for (int j = 0; j < sampleSize; j++)
                {
                    randomArray[j] = rnd.Next(1, 100);
                }

                result = normalityTestFunc(randomArray);
                Console.WriteLine("randomArray_" + i + ": " + result);
            }

            Console.WriteLine();

            // Uniform distribution
            Console.WriteLine("rand var");
            var uniformSamples = new Normal(100, 0).Samples().Take(sampleSize);

            double[] uniformSamplesRandomVar = uniformSamples.ToArray();

            result = normalityTestFunc(uniformSamplesRandomVar);
            Console.WriteLine("uniformSamples: " + result);
        }

        [Test][STAThread]
        public void PeakNormalityTest()
        {
            NormalityTest.NormalityTestFunc normalityTestFunc = NormalityTest.JaqueBeraTest;

            double result = 0;

            int sampleSize = 100;

            // Good shaped peaks
            Console.WriteLine("Peaks with relatively good shape");

            double[] sampleTypical = 
            { 
                 203, 503, 477, 621, 710, 581, 554, 329, 480, 382
            };

            result = NormalityTest.PeakNormalityTest(sampleTypical, normalityTestFunc, sampleSize, 10000);
            Console.WriteLine("sampleTypical: " + result);

            double[] sampleActualPeak = 
            { 
                 0.203, 0.382, 0.477, 0.48, 0.54, 0.62, 0.54, 0.48, 0.382, 0.203
            };

            result = NormalityTest.PeakNormalityTest(sampleActualPeak, normalityTestFunc, sampleSize, 100);
            Console.WriteLine("sampleActualPeak: " + result);

            // Subjective shapes
            Console.WriteLine();
            Console.WriteLine("Peaks with subjective shapes");

             double[] sampleAll1s =
            { 
                 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 
            };

            result = NormalityTest.PeakNormalityTest(sampleAll1s, normalityTestFunc, sampleSize, 100);
            Console.WriteLine("sampleAll1s: " + result);
            
            double[] sampleAll0s =
            { 
                 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            result = NormalityTest.PeakNormalityTest(sampleAll0s, normalityTestFunc, sampleSize, 100);
            Console.WriteLine("sampleAll0s: " + result);

            double[] smallSample =
            { 
                 0, 1, 0
            };

            result = NormalityTest.PeakNormalityTest(smallSample, normalityTestFunc, sampleSize, 100);
            Console.WriteLine("smallSample: " + result);

            // Bad shaped peaks
            Console.WriteLine();
            Console.WriteLine("Peaks with relatively bad shape");

            double[] doublePeak =
            { 
                  0.203, 0.382, 200, 1, 0.54, 200, 0, 0.48, 0.382, 0.203
            };

            result = NormalityTest.PeakNormalityTest(doublePeak, normalityTestFunc, sampleSize, 100);
            Console.WriteLine("doublePeak: " + result);

            double[] kindaLikeNoise =
            { 
                 0.203, 0.503, 0.477, 0.621, 0.710, 200, 0.554, 0.329, 0.480, 0.382
            };

            result = NormalityTest.PeakNormalityTest(kindaLikeNoise, normalityTestFunc, sampleSize, 100);
            Console.WriteLine("kindaLikeNoise: " + result);
        }

        /// <summary>
        /// The bps.
        /// </summary>
        public const string BPSNegative = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-BPS_neg2_28Aug14_Columbia_DI.uimf";

        /// <summary>
        /// The cae.
        /// </summary>
        public const string Cae = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-CAE_pos2_9Oct14_Columbia_DI.uimf";

        // <summary>
         // The test scoring.
         // </summary>
         [Test][STAThread]
         public void TestScoring()
         {
             string formula = "C9H13ClN6";
             string fileLocation = Cae;
             MolecularTarget target = new MolecularTarget(formula, IonizationMethod.ProtonPlus, "CAE");
             
             Console.WriteLine("CompositionWithoutAdduct: " + target.CompositionWithoutAdduct);
             Console.WriteLine("Monoisotopic ViperCompatibleMass: " + target.MonoisotopicMass);
         
             CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();
         
             var smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 2);
         
             CrossSectionWorkfow workflow = new CrossSectionWorkfow(fileLocation, "output", parameters);
         
             Console.WriteLine("Ionization method: " + target.Adduct);
             Console.WriteLine("Targeting centerMz: " + target.MassWithAdduct);
                 
             // Generate Theoretical Isotopic Profile
             List<Peak> theoreticalIsotopicProfilePeakList = null;
             if (target.CompositionWithAdduct != null) 
             {
                 string empiricalFormula = target.CompositionWithAdduct.ToPlainString();
                 var theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();
                 IsotopicProfile theoreticalIsotopicProfile = theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, 1);
                 theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
             }
             
             // Generate VoltageSeparatedAccumulatedXICs
             var uimfReader = new DataReader(fileLocation);
             Console.WriteLine("Input file: {0}", fileLocation);
             VoltageSeparatedAccumulatedXiCs accumulatedXiCs = new VoltageSeparatedAccumulatedXiCs(uimfReader, target.MassWithAdduct, parameters.MzWindowHalfWidthInPpm);
         
             Console.WriteLine();
         
             // For each voltage, find 2D XIC features 
             foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
             {
                 Console.WriteLine("Voltage group: {0} V, Frame {1}-{2}, {3:F2}K, {4:F2}Torr", 
                     voltageGroup.MeanVoltageInVolts, 
                     voltageGroup.FirstFirstFrameNumber, 
                     voltageGroup.LastFrameNumber,
                     voltageGroup.MeanTemperatureInKelvin,
                     voltageGroup.MeanPressureInTorr);
         
                 List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;
                 List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, smoother, parameters.FeatureFilterLevel);
                 List<StandardImsPeak> standardPeaks = featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, uimfReader, voltageGroup,  target.MassWithAdduct, parameters.MzWindowHalfWidthInPpm)).ToList();
         
                 // feature scorings and Target selection.
                 double globalMaxIntensity = IMSUtil.MaxIntensityAfterFrameAccumulation(voltageGroup, uimfReader);
         
                 // Check each XIC Peak found
                 foreach (var featurePeak in standardPeaks)
                 {
                     // Evaluate feature scores.
                    double intensityScore = FeatureScoreUtilities.IntensityScore(featurePeak, globalMaxIntensity);
                     
                    double isotopicScoreAngle = FeatureScoreUtilities.IsotopicProfileScore(
                         featurePeak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.Angle, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
         
                     double isotopicScoreDistance = FeatureScoreUtilities.IsotopicProfileScore(
                         featurePeak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.EuclideanDistance, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
         
                     double isotopicScorePerson = FeatureScoreUtilities.IsotopicProfileScore(
                         featurePeak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.PearsonCorrelation, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
         
                     double isotopicScoreBhattacharyya = FeatureScoreUtilities.IsotopicProfileScore(
                         featurePeak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.Bhattacharyya, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
         
                     double isotopicScoreDistanceAlternative = FeatureScoreUtilities.IsotopicProfileScore(
                         featurePeak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.EuclideanDistanceAlternative, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
                     
                     double peakShapeScore = FeatureScoreUtilities.PeakShapeScore(featurePeak, workflow.uimfReader, workflow.Parameters.MzWindowHalfWidthInPpm, workflow.Parameters.DriftTimeToleranceInMs, voltageGroup, globalMaxIntensity, workflow.NumberOfScans);
                     
                     // Report all features.
                     Console.WriteLine(" feature found at scan number {0}", featurePeak.HighestPeakApex.DriftTimeCenterInScanNumber);
                     Console.WriteLine("     IntensityScore: {0}", intensityScore);
                     Console.WriteLine("     peakShapeScore: {0}", peakShapeScore);
                     Console.WriteLine("     isotopicScore - Angle:    {0}", isotopicScoreAngle);
                     Console.WriteLine("     isotopicScore - Distance: {0}", isotopicScoreDistance);
                     Console.WriteLine("     isotopicScore - Distance2:{0}", isotopicScoreDistanceAlternative);
                     Console.WriteLine("     isotopicScore - Pearson:  {0}", isotopicScorePerson);
                     Console.WriteLine("     isotopicScore - Bhattacharyya: {0}", isotopicScoreBhattacharyya);
                     
                     Console.WriteLine();
                 }
         
                 Console.WriteLine();
             }
         
             workflow.Dispose();
         }

        /// <summary>
        /// The test scoring.
        /// </summary>
         [Test][STAThread]
         public void TestFormulaPerturbance()
         {
             List<Tuple<string, string>> formulas = new List<Tuple<string, string>>();
             
             // truth
             formulas.Add(new Tuple<string, string>("True formula", "C12H10O4S"));
             formulas.Add(new Tuple<string, string>("1 extra H", "C12H11O4S"));
             formulas.Add(new Tuple<string, string>("2 extra H", "C12H12O4S"));
             formulas.Add(new Tuple<string, string>("3 extra H", "C12H13O4S"));
             formulas.Add(new Tuple<string, string>("3 extra H", "C12H14O4S"));
             formulas.Add(new Tuple<string, string>("4 extra H", "C12H15O4S"));
             formulas.Add(new Tuple<string, string>("5 extra H", "C12H16O4S"));
             formulas.Add(new Tuple<string, string>("1 less H", "C12H9O4S"));
             formulas.Add(new Tuple<string, string>("2 less H", "C12H8O4S"));
             formulas.Add(new Tuple<string, string>("3 less H", "C12H7O4S"));
             formulas.Add(new Tuple<string, string>("4 less H", "C12H6O4S"));
             Console.WriteLine("[Intensity], [Distance1], [Distance2], [Angle], [Pearson], [Bucha]");
         
             string fileLocation = BPSNegative;
             CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();
             CrossSectionWorkfow workflow = new CrossSectionWorkfow(fileLocation, "output", parameters);
         
             foreach (var form in formulas)
             {
                 bool found = false;
                 
                 MolecularTarget target = new MolecularTarget(form.Item2, new IonizationAdduct(IonizationMethod.ProtonMinus), form.Item1);
                 Console.Write(form.Item1 + ": ");
                 var smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 2);
         
                 // Generate Theoretical Isotopic Profile
                 List<Peak> theoreticalIsotopicProfilePeakList = null;
                 if (target.CompositionWithAdduct != null) 
                 {
                     string empiricalFormula = target.CompositionWithAdduct.ToPlainString();
                     var theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();
                     IsotopicProfile theoreticalIsotopicProfile = theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, 1);
                     theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
                 }
                 
                 // Generate VoltageSeparatedAccumulatedXICs
                 var uimfReader = new DataReader(fileLocation);
                 VoltageSeparatedAccumulatedXiCs accumulatedXiCs = new VoltageSeparatedAccumulatedXiCs(uimfReader, target.MassWithAdduct, parameters.MzWindowHalfWidthInPpm);
         
                 var voltageGroup = accumulatedXiCs.Keys.First();
         
                 // Find peaks using multidimensional peak finder.
                 List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;
                 List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, smoother, parameters.FeatureFilterLevel);
                 List<StandardImsPeak> standardPeaks = featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, uimfReader, voltageGroup,  target.MassWithAdduct, parameters.MzWindowHalfWidthInPpm)).ToList();
         
                 // feature scorings and Target selection.
                 double globalMaxIntensity = IMSUtil.MaxIntensityAfterFrameAccumulation(voltageGroup, uimfReader);
         
                 // Check each XIC Peak found
                 foreach (var peak in standardPeaks)
                 {
                     // Evaluate feature scores.
                     double intensityScore = FeatureScoreUtilities.IntensityScore(peak, globalMaxIntensity);
                     
                     double isotopicScoreAngle = FeatureScoreUtilities.IsotopicProfileScore(
                         peak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.Angle, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
         
                     double isotopicScoreDistance = FeatureScoreUtilities.IsotopicProfileScore(
                         peak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.EuclideanDistance, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
         
                     double isotopicScorePerson = FeatureScoreUtilities.IsotopicProfileScore(
                         peak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.PearsonCorrelation, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
         
                     double isotopicScoreBhattacharyya = FeatureScoreUtilities.IsotopicProfileScore(
                         peak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.Bhattacharyya, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
         
                     double isotopicScoreDistanceAlternative = FeatureScoreUtilities.IsotopicProfileScore(
                         peak, 
                         workflow.uimfReader, 
                         target, 
                         theoreticalIsotopicProfilePeakList, 
                         voltageGroup, 
                         IsotopicScoreMethod.EuclideanDistanceAlternative, 
                         globalMaxIntensity, 
                         workflow.NumberOfScans);
                     
                     double peakShapeScore = FeatureScoreUtilities.PeakShapeScore(peak, workflow.uimfReader, workflow.Parameters.MzWindowHalfWidthInPpm, workflow.Parameters.DriftTimeToleranceInMs, voltageGroup, globalMaxIntensity, workflow.NumberOfScans);
                     
                     // Report all features.
                     if (peak.HighestPeakApex.DriftTimeCenterInScanNumber == 115)
                     {
                         Console.Write("{0:F4} ", intensityScore);
                         found = true;
                     }
         
                     // Report all features.
                     if (peak.HighestPeakApex.DriftTimeCenterInScanNumber == 115)
                     {
                         Console.Write("{0:F4} ", isotopicScoreDistance);
                         found = true;
                     }
         
                     // Report all features.
                     if (peak.HighestPeakApex.DriftTimeCenterInScanNumber == 115)
                     {
                         Console.Write("{0:F4} ", isotopicScoreDistanceAlternative);
                         found = true;
                     }
         
                     // Report all features.
                     if (peak.HighestPeakApex.DriftTimeCenterInScanNumber == 115)
                     {
                         Console.Write("{0:F4} ", isotopicScoreAngle);
                         found = true;
                     }
         
                     // Report all features.
                     if (peak.HighestPeakApex.DriftTimeCenterInScanNumber == 115)
                     {
                         Console.Write("{0:F4} ", isotopicScorePerson);
                         found = true;
                     }
         
                     // Report all features.
                     if (peak.HighestPeakApex.DriftTimeCenterInScanNumber == 115)
                     {
                         Console.Write("{0:F4} ", isotopicScoreBhattacharyya);
                         found = true;
                     }
                 }
         
                 if (!found)
                 {
                     Console.Write("No features");
                 }
         
                 Console.WriteLine();
             }
         
             // Manually dispose so it doesn't interfere with other tests.
             workflow.Dispose();
         }
    }
}
