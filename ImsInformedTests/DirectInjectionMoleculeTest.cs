// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirectInjectionMoleculeTest.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2015 Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The direct injection molecule test.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformedTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using DeconTools.Backend.Core;
    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;
    using DeconTools.Backend.Utilities;

    using ImsInformed.Domain;
    using ImsInformed.Parameters;
    using ImsInformed.Scoring;
    using ImsInformed.Stats;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

    using MathNet.Numerics.Distributions;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using NUnit.Framework;

    using UIMFLibrary;

    /// <summary>
    /// The direct injection molecule test.
    /// </summary>
    public class DirectInjectionMoleculeTest
    {
        /// <summary>
        /// The nicotine UIMF file.
        /// </summary>
        public const string NicoFile = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\uimf_files\smallMolecule\EXP-NIC_neg2_28Aug14_Columbia_DI.uimf";

        /// <summary>
        /// The Acetamiprid file.
        /// </summary>
        public const string AcetamipridFile = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\uimf_files\smallMolecule\EXP-AAP_neg_26Aug14_Columbia_DI.uimf";

        /// <summary>
        /// The acetaminophen.
        /// </summary>
        public const string Acetaminophen = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\uimf_files\smallMolecule\EXP-ACE_neg2_28Aug14_Columbia_DI.uimf";

        /// <summary>
        /// The bps.
        /// </summary>
        public const string Bps = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\uimf_files\smallMolecule\EXP-BPS_neg2_28Aug14_Columbia_DI.uimf";

                /// <summary>
        /// The test empirical formula utilities.
        /// </summary>
        [Test]
        public void TestEmpiricalFormulaUtilities()
        {
            List<string> testCases = new List<string>();
            testCases.Add("FeS");
            testCases.Add("C18H24N2O10P2S2");
            testCases.Add("c18H24n2O1P2s2"); //case test
            testCases.Add("Jian22TNT250"); //Random string
            testCases.Add("(NH4)2SO4");
            testCases.Add("(CF3)2C(C6H4OH)2");
            testCases.Add("O2S[C6H3(CH3)OH]2");

            // More complicated test cases with parenthesis
            foreach (string testCase in testCases)
            {
                Console.WriteLine(testCase);
                try
                {
                    Composition compo = Composition.ParseFromPlainString(testCase);
                    Console.WriteLine(@"InformedProteomicsParse: [{0}]", compo.ToPlainString());
                } 
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                try
                {
                    
                    Dictionary<string, int> dict = EmpiricalFormulaUtilities.ParseEmpiricalFormulaString(testCase);
                    Console.WriteLine(@"Decon tool ParseEmpiricalFormulaString:");
                    foreach (var entry in dict)
                    {
                        Console.Write(@"[{0} {1}]", entry.Key, entry.Value);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
                Console.WriteLine("\r\n");
            }
        }

        /// <summary>
        /// The test molecule util.
        /// </summary>
        [Test]
        public void TestMoleculeUtil()
        {
            List<string> testCases = new List<string>();
            testCases.Add("FeS");
            
            testCases.Add("C18H24N2O10P2S2");
            testCases.Add("c18H24n2O1P2s2"); //case test
            testCases.Add("CCl4");
            
            testCases.Add("(NH4)2SO4");
            testCases.Add("(CF3)2C(C6H4OH)2");
            testCases.Add("O2S[C6H3(CH3)OH]2");
            
            //invalid inputs
            testCases.Add("(CH3)3CCH2C(CH3)2C6H4OH");
            testCases.Add("Mg(NO3)2");
            testCases.Add("Jian22TNT250"); //Random string

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
        /// The test single molecule with formula.
        /// </summary>
        [Test][STAThread]
        public void TestSingleMoleculeWithFormula()
        {
            // Nicotine
            // string formula = "C10H14N2";
            // ImsTarget sample = new ImsTarget(1, IonizationMethod.Proton2Plus, formula);
            // Console.WriteLine("MZ:   " +  221.0594);
            // string fileLocation = AcetamipridFile;

            // Acetamiprid
            // string formula = "C10H11ClN4";
            // ImsTarget sample = new ImsTarget(1, IonizationMethod.ProtonMinus, formula);
            // Console.WriteLine("MZ:   " +  221.0594);
            // string fileLocation = AcetamipridFile;

            // BPS Na
            // string formula = "C12H10O4S";
            // ImsTarget sample = new ImsTarget(1, IonizationMethod.ProtonMinus, formula);
            // string fileLocation = Bps;

            string formula = "C8H18O";
            ImsTarget sample = new ImsTarget(1, IonizationMethod.ProtonMinus, formula);
            string fileLocation = @"\\protoapps\ims08_UIMFs\EXP-2EH_neg2_2Sep14_Columbia_DI.uimf";


            Console.WriteLine("Dataset: {0}", fileLocation);
            Console.WriteLine("Composition: " + sample.Composition);
            Console.WriteLine("Monoisotopic Mass: " + sample.Mass);

            MoleculeWorkflowParameters parameters = new MoleculeWorkflowParameters 
            {
                MassToleranceInPpm = 10,
                NumPointForSmoothing = 9,
                ScanWindowWidth = 4,
            };

            MoleculeInformedWorkflow informedWorkflow = new MoleculeInformedWorkflow(fileLocation, "output", "result.txt", parameters);
            informedWorkflow.RunMoleculeInformedWorkFlow(sample);
        }

        /// <summary>
        /// The test single molecule MZ only.
        /// </summary>
        [Test][STAThread]
        public void TestSingleMoleculeMzOnly()
        {
            // Good BPS data
            double mz = 249.02160599;
            mz = 399;
            string uimfFile = DirectInjectionMoleculeTest.Bps;

            // Acetaminophen
            // double mz = 150.0555008;
            // string uimfFile = Acetaminophen;

            // Nicotinefnic
            // double mz = 161.10787;
            // string uimfFile = NicoFile;

            // Nico M+H
            // double mz = 161.10787;
            // string uimfFile = NicoFile;

            // AcetamipridFile
            // double mz = 221.059395;
            // string uimfFile = AcetamipridFile;

            ImsTarget target= new ImsTarget(1, IonizationMethod.ProtonMinus, mz);
            Console.WriteLine("Nicotine:");
            Console.WriteLine("MZ:   " + mz);

            MoleculeWorkflowParameters parameters = new MoleculeWorkflowParameters 
            {
                MassToleranceInPpm = 10,
                NumPointForSmoothing = 9,
                ScanWindowWidth = 4,
            };

            MoleculeInformedWorkflow workflow = new MoleculeInformedWorkflow(uimfFile, "output", "result.txt", parameters);
            workflow.RunMoleculeInformedWorkFlow(target);
        }

        /// <summary>
        /// The bytes.
        /// </summary>
        [Test]
        public void Bytes() 
        {
            byte[] array = new byte[4] { 5, 6, 7, 100 };
            Int32 a = BitConverter.ToInt32(array,0);
            Int32 b = 0x01;
            Int32 c =  0x010000000;
            array = BitConverter.GetBytes(c);
            Console.WriteLine("{0:x8} vs {1:x8}", b, c);
            Console.WriteLine( 0x01 == 0x010000000 );
        }

        /// <summary>
        /// The test file not found.
        /// </summary>
        [Test]
        public void TestFileNotFound()
        {
            // Good BPS data
            double mz = 273.0192006876;
            string uimfFile = "blablabla";

            // Acetaminophen
            // double mz = 150.0555008;
            // string uimfFile = Acetaminophen;

            // Nicotinefnic
            // double mz = 161.10787;
            // string uimfFile = NicoFile;

            // AcetamipridFile
            // double mz = 221.059395;
            // string uimfFile = AcetamipridFile;

            ImsTarget target= new ImsTarget(1, IonizationMethod.ProtonMinus, mz);
            Console.WriteLine("Nicotine:");
            Console.WriteLine("MZ:   " + mz);

            MoleculeWorkflowParameters parameters = new MoleculeWorkflowParameters 
            {
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 10,
                NumPointForSmoothing = 9
            };

            Assert.Throws<FileNotFoundException>(() => new MoleculeInformedWorkflow(uimfFile, "output", "result.txt", parameters));
        }

        /// <summary>
        /// The test locked uimf.
        /// </summary>
        [Test]
        public void TestLockedUIMF()
        {
            // A locked UIMF file
            string uimfFile = @"\\protoapps\ims08_UIMFs\EXP-NIC_pos2_13Sep14_Columbia_DI.uimf";

            ImsTarget target = new ImsTarget(1, IonizationMethod.SodiumPlus, "C10H14N2");

            MoleculeWorkflowParameters parameters = new MoleculeWorkflowParameters 
            {
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 10,
                NumPointForSmoothing = 9
            };

            MoleculeInformedWorkflow workflow = new MoleculeInformedWorkflow(uimfFile, "output", "result.txt", parameters);
            workflow.RunMoleculeInformedWorkFlow(target);
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
                distribution.RandomSource = new Random(System.DateTime.Now.Millisecond * i);
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

            double significantLevel = 0.05;

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
        /// The test single molecule MZ only.
        /// </summary>
        [Test][STAThread]
        public void TestSingleMoleculeBadTarget()
        {
            string formula = "NotAFormula";
            Assert.Throws<Exception>(() => new ImsTarget(1, IonizationMethod.ProtonMinus, formula));
        }

        /// <summary>
        /// The test scoring.
        /// </summary>
        [Test][STAThread]
        public void TestFormulaPerturbance()
        {
            List<string> formulas = new List<string>();
            // truth
            formulas.Add("C12H10O4S");
            formulas.Add("C12H11O4S");
            formulas.Add("C12H12O4S");
            formulas.Add("C12H13O4S");
            formulas.Add("C12H14O4S");
            formulas.Add("C12H15O4S");
            formulas.Add("C12H16O4S");
            formulas.Add("C12H9O4S ");
            formulas.Add("C12H8O4S ");
            formulas.Add("C12H7O4S ");
            formulas.Add("C12H6O4S ");
            foreach (var form in formulas)
            {
                bool found = false;
                string formula = form;
                string fileLocation = Bps;
                ImsTarget sample = new ImsTarget(1, IonizationMethod.ProtonMinus, formula);
                
                MoleculeWorkflowParameters parameters = new MoleculeWorkflowParameters 
                {
                    MassToleranceInPpm = 5,
                    NumPointForSmoothing = 9,
                    ScanWindowWidth = 4,
                };

                var smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 2);

                MoleculeInformedWorkflow informedWorkflow = new MoleculeInformedWorkflow(fileLocation, "output", "result.txt", parameters);

                // ImsTarget assumes proton+ ionization because it's designed for peptides. Get rid of it here.
                Composition targetComposition = MoleculeUtil.IonizationCompositionCompensation(sample.Composition, sample.IonizationType);
                targetComposition = MoleculeUtil.IonizationCompositionDecompensation(targetComposition, IonizationMethod.ProtonPlus);

                // Setup target object
                if (targetComposition != null) 
                {
                    // Because Ion class from Informed Proteomics assumes adding a proton, that's the reason for decompensation
                    Ion targetIon = new Ion(targetComposition, 1);
                    sample.TargetMz = targetIon.GetMonoIsotopicMz();
                } 
                
                // Generate Theoretical Isotopic Profile
                List<Peak> theoreticalIsotopicProfilePeakList = null;
                if (targetComposition != null) 
                {
                    string empiricalFormula = targetComposition.ToPlainString();
                    var theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();
                    IsotopicProfile theoreticalIsotopicProfile = theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, 1);
                    theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
                }
                
                // Generate VoltageSeparatedAccumulatedXICs
                var uimfReader = new DataReader(fileLocation);
                VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(uimfReader, sample.TargetMz, parameters);

                var voltageGroup = accumulatedXiCs.Keys.First();
                // Smooth Chromatogram
                IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(accumulatedXiCs[voltageGroup].IntensityPoints);
                smoother.Smooth(ref pointList);
                
                // Peak Find Chromatogram
                IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);

                // pre-filtering: reject small feature peaks. Fast filtering.
                featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, parameters.FeatureFilterLevel);

                // feature scorings and target selection.
                FeatureBlob bestFeature = null;
                FeatureScoreHolder mostLikelyPeakScores;
                mostLikelyPeakScores.IntensityScore = 0;
                mostLikelyPeakScores.IsotopicScore = 0;
                mostLikelyPeakScores.PeakShapeScore = 0;
                double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, uimfReader);

                // Check each XIC Peak found
                foreach (var featureBlob in featureBlobs)
                {
                    // Evaluate feature scores.
                    double intensityScore = FeatureScores.IntensityScore(informedWorkflow, featureBlob, voltageGroup, globalMaxIntensity);
                    
                    double isotopicScoreAngle = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.Angle);

                    double isotopicScoreDistance = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.EuclideanDistance);

                    double isotopicScorePerson = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.PearsonCorrelation);

                    double isotopicScoreBhattacharyya = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.Bhattacharyya);

                    double isotopicScoreDistanceAlternative = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.EuclideanDistanceAlternative);
                    
                    double peakShapeScore = FeatureScores.PeakShapeScore(informedWorkflow, featureBlob.Statistics, voltageGroup, sample.TargetMz, globalMaxIntensity);
                    
                    // Report all features.
                    if (featureBlob.Statistics.ScanImsRep == 115)
                    {
                        Console.WriteLine("{0}", isotopicScoreBhattacharyya);
                        found = true;
                    }
                }

                if (!found)
                {
                    Console.WriteLine("0");
                }
            }
        }

        /// <summary>
        /// The test scoring.
        /// </summary>
        [Test][STAThread]
        public void TestScoring()
        {
            string formula = "C12H11O4S";
            string fileLocation = Bps;
            ImsTarget sample = new ImsTarget(1, IonizationMethod.ProtonMinus, formula);
            
            Console.WriteLine("Dataset: {0}", fileLocation);
            Console.WriteLine("Composition: " + sample.Composition);
            Console.WriteLine("Monoisotopic Mass: " + sample.Mass);

            MoleculeWorkflowParameters parameters = new MoleculeWorkflowParameters 
            {
                MassToleranceInPpm = 5,
                NumPointForSmoothing = 9,
                ScanWindowWidth = 4,
            };

            var smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 2);

            MoleculeInformedWorkflow informedWorkflow = new MoleculeInformedWorkflow(fileLocation, "output", "result.txt", parameters);

            // ImsTarget assumes proton+ ionization because it's designed for peptides. Get rid of it here.
            Composition targetComposition = MoleculeUtil.IonizationCompositionCompensation(sample.Composition, sample.IonizationType);
            targetComposition = MoleculeUtil.IonizationCompositionDecompensation(targetComposition, IonizationMethod.ProtonPlus);

            // Setup target object
            if (targetComposition != null) 
            {
                // Because Ion class from Informed Proteomics assumes adding a proton, that's the reason for decompensation
                Ion targetIon = new Ion(targetComposition, 1);
                sample.TargetMz = targetIon.GetMonoIsotopicMz();
            } 
            
            Console.WriteLine("Ionization method: " + sample.IonizationType);
            Console.WriteLine("Targeting Mz: " + sample.TargetMz);
                
            // Generate Theoretical Isotopic Profile
            List<Peak> theoreticalIsotopicProfilePeakList = null;
            if (targetComposition != null) 
            {
                string empiricalFormula = targetComposition.ToPlainString();
                var theoreticalFeatureGenerator = new JoshTheorFeatureGenerator();
                IsotopicProfile theoreticalIsotopicProfile = theoreticalFeatureGenerator.GenerateTheorProfile(empiricalFormula, 1);
                theoreticalIsotopicProfilePeakList = theoreticalIsotopicProfile.Peaklist.Cast<Peak>().ToList();
            }
            
            // Generate VoltageSeparatedAccumulatedXICs
            var uimfReader = new DataReader(fileLocation);
            Console.WriteLine("Input file: {0}", fileLocation);
            VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(uimfReader, sample.TargetMz, parameters);

            Console.WriteLine();

            // For each voltage, find 2D XIC features 
            foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
            {
                Console.WriteLine("Voltage group: {0} V, [{1}-{2}]", voltageGroup.MeanVoltageInVolts, voltageGroup.FirstFrameNumber, voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount);

                // Smooth Chromatogram
                IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(accumulatedXiCs[voltageGroup].IntensityPoints);
                smoother.Smooth(ref pointList);
                
                // Peak Find Chromatogram
                IEnumerable<FeatureBlob> featureBlobs = FeatureDetection.DoWatershedAlgorithm(pointList);

                // pre-filtering: reject small feature peaks. Fast filtering.
                featureBlobs = FeatureDetection.FilterFeatureList(featureBlobs, parameters.FeatureFilterLevel);

                // feature scorings and target selection.
                FeatureBlob bestFeature = null;
                FeatureScoreHolder mostLikelyPeakScores;
                mostLikelyPeakScores.IntensityScore = 0;
                mostLikelyPeakScores.IsotopicScore = 0;
                mostLikelyPeakScores.PeakShapeScore = 0;
                double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, uimfReader);

                // Check each XIC Peak found
                foreach (var featureBlob in featureBlobs)
                {
                    // Evaluate feature scores.
                    double intensityScore = FeatureScores.IntensityScore(informedWorkflow, featureBlob, voltageGroup, globalMaxIntensity);
                    
                    double isotopicScoreAngle = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.Angle);

                    double isotopicScoreDistance = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.EuclideanDistance);

                    double isotopicScorePerson = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.PearsonCorrelation);

                    double isotopicScoreBhattacharyya = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.Bhattacharyya);

                    double isotopicScoreDistanceAlternative = FeatureScores.IsotopicProfileScore(
                            informedWorkflow, 
                            sample, 
                            featureBlob.Statistics, 
                            theoreticalIsotopicProfilePeakList, 
                            voltageGroup, IsotopicScoreMethod.EuclideanDistanceAlternative);
                    
                    double peakShapeScore = FeatureScores.PeakShapeScore(informedWorkflow, featureBlob.Statistics, voltageGroup, sample.TargetMz, globalMaxIntensity);
                    
                    // Report all features.
                    Console.WriteLine(" feature found at scan number {0}", featureBlob.Statistics.ScanImsRep);
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
        }
    }
}
