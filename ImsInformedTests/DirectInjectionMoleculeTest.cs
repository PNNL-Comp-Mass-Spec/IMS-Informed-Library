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
    using System.IO;
    using System.Linq;

    using DeconTools.Backend.Core;
    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;
    using DeconTools.Backend.Utilities;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Interfaces;
    using ImsInformed.IO;
    using ImsInformed.Scoring;
    using ImsInformed.Stats;
    using ImsInformed.Targets;
    using ImsInformed.Util;
    using ImsInformed.Workflows.CrossSectionExtraction;
    using ImsInformed.Workflows.DriftTimeLibraryMatch;
    using ImsInformed.Workflows.VoltageAccumulation;

    using InformedProteomics.Backend.Data.Composition;

    using MathNet.Numerics.Distributions;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using NUnit.Framework;

    using UIMFLibrary;

    using Peak = DeconTools.Backend.Core.Peak;
    using PeptideTarget = ImsInformed.Targets.PeptideTarget;

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
        public const string AcetamipridFile = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-AAP_neg_26Aug14_Columbia_DI.uimf";

        /// <summary>
        /// The mix 1.
        /// </summary>
        public const string Mix1 = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\mix\Mix1_8Oct13_Columbia_DI.uimf";

        /// <summary>
        /// The mix 1.
        /// </summary>
        public const string F1E = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-F1E_pos_10Oct14_Columbia_DI.uimf";

        /// <summary>
        /// The acetaminophen.
        /// </summary>
        public const string Acetaminophen = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-ACE_neg2_28Aug14_Columbia_DI.uimf";

        /// <summary>
        /// The bps.
        /// </summary>
        public const string BPSNegative = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-BPS_neg2_28Aug14_Columbia_DI.uimf";

        /// <summary>
        /// The bps postive.
        /// </summary>
        public const string BPSPostive = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-BPS_pos2_13Sep14_Columbia_DI.uimf";
            
        /// <summary>
        /// The cae.
        /// </summary>
        public const string Cae = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-CAE_pos2_9Oct14_Columbia_DI.uimf";

        /// <summary>
        /// The amt lib pos.
        /// </summary>
        public const string AmtLibPos = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\example_library_files\viper_pos_chemical_based.txt";

                /// <summary>
        /// The test empirical formula utilities.
        /// </summary>
        [Test]
        public void TestEmpiricalFormulaUtilities()
        {
            List<string> testCases = new List<string>();
            testCases.Add("C16D10");
            testCases.Add("CH3COOH");
            testCases.Add("FeS");
            testCases.Add(" C13H13ClN4O2S");
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
        /// The test single molecule with formula.
        /// </summary>
        [Test][STAThread]
        public void TestSingleMoleculeWithFormula()
        {
            // Nicotine
            // string formula = "C10H14N2";
            // ImsTarget sample = new ImsTarget(1, Adduct.ProtonPlus, formula);
            // Console.WriteLine("MZ:   " +  221.0594);
            // string fileLocation = AcetamipridFile;

            // Acetamiprid
            // string formula = "C10H11ClN4";
            // ImsTarget sample = new ImsTarget(1, Adduct.ProtonMinus, formula);
            // string fileLocation = AcetamipridFile;

            // F1E
            // string formula = "C15H21Cl2FN2O3";
            // MolecularTarget sample = new MolecularTarget(formula, IonizationMethod.ProtonPlus);
            // string fileLocation = F1E;

            // BPS Negative
            // string formula = "C12H10O4S";
            // MolecularTarget sample = new MolecularTarget(formula, IonizationMethod.prot);
            // string fileLocation = BPSNegative;

            // BPS Positive
            string formula = "C12H10O4S";
            MolecularTarget sample = new MolecularTarget(formula, IonizationMethod.ProtonPlus, "BPS");
            string fileLocation = BPSPostive;

            Console.WriteLine("Dataset: {0}", fileLocation);

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            CrossSectionWorkfow workfow = new CrossSectionWorkfow(fileLocation, "output", parameters);
            workfow.RunCrossSectionWorkFlow(sample, true);
            workfow.Dispose();
        }

        /// <summary>
        /// The test single molecule MZ only.
        /// </summary>
        [Test][STAThread]
        public void TestSingleMoleculeMzOnly()
        {
            // Good BPS data
            double mz = 251.037804609;
            string uimfFile = BPSPostive;

            // Acetaminophen
            // double mz = 150.0555008;
            // string uimfFile = Acetaminophen;

            // Nicotinefnic
            // double mz = 161.10787;
            // string uimfFile = NicoFile;

            // Nico M+H
            // double mz = 161.10787;
            // string uimfFile = NicoFile;

            MolecularTarget target= new MolecularTarget(mz, IonizationMethod.ProtonPlus, "Nicotine");

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            CrossSectionWorkfow workflow = new CrossSectionWorkfow(uimfFile, "output",  parameters);
            workflow.RunCrossSectionWorkFlow(target);
            workflow.Dispose();
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
            Console.WriteLine(0x01 == 0x010000000);
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

            MolecularTarget target = new MolecularTarget(mz, IonizationMethod.ProtonMinus, "BPS");

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            Assert.Throws<FileNotFoundException>(() => new CrossSectionWorkfow(uimfFile, "output", parameters));
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
            Assert.Throws<Exception>(() => new MolecularTarget(formula, IonizationMethod.ProtonMinus, "Magic molecule"));
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
                VoltageSeparatedAccumulatedXiCs accumulatedXiCs = new VoltageSeparatedAccumulatedXiCs(uimfReader, target.MassWithAdduct, parameters.MassToleranceInPpm);

                var voltageGroup = accumulatedXiCs.Keys.First();

                // Find peaks using multidimensional peak finder.
                List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;
                List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, smoother, parameters.FeatureFilterLevel);
                List<StandardImsPeak> standardPeaks = featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, uimfReader, voltageGroup, target.MassWithAdduct, parameters.MassToleranceInPpm)).ToList();

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
                    
                    double peakShapeScore = FeatureScoreUtilities.PeakShapeScore(peak, workflow.uimfReader, workflow.Parameters.MassToleranceInPpm, workflow.Parameters.DriftTimeToleranceInMs, voltageGroup, globalMaxIntensity, workflow.NumberOfScans);
                    
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

         /// <summary>
        /// The test single molecule MZ only.
        /// </summary>
        [Test][STAThread]
        public void TestMixedSampleLibraryMatching()
        {
            // TODO Import AMT library instead of manually add them
            IList<DriftTimeTarget> imsTargets = new List<DriftTimeTarget>();
            DriftTimeTarget t1 = new DriftTimeTarget("BPS protonated", 23.22, "C12H10O4S", IonizationMethod.ProtonPlus);
            DriftTimeTarget t2 = new DriftTimeTarget("BPS sodiated", 31.8506, "C12H10O4S", IonizationMethod.SodiumPlus);
            DriftTimeTarget t3 = new DriftTimeTarget("I made it up", 15, "C14H14O4S", IonizationMethod.SodiumPlus);
            DriftTimeTarget t4 = new DriftTimeTarget("I made it up again", 15, "C12H11O4S", IonizationMethod.SodiumPlus);
            
            imsTargets.Add(t3);
            imsTargets.Add(t1);
            imsTargets.Add(t2);
            imsTargets.Add(t4);

            LibraryMatchWorkflow workflow = new LibraryMatchWorkflow(BPSPostive, "output", "result.txt", new LibraryMatchParameters());
            IDictionary<DriftTimeTarget, LibraryMatchResult> results = workflow.RunLibraryMatchWorkflow(imsTargets);
            Assert.AreEqual(results[t1].AnalysisStatus, AnalysisStatus.Positive);
            Assert.AreEqual(results[t2].AnalysisStatus, AnalysisStatus.Positive);
            Assert.AreEqual(results[t3].AnalysisStatus, AnalysisStatus.Negative);
            Assert.AreEqual(results[t4].AnalysisStatus, AnalysisStatus.Negative);
        }

        /// <summary>
        /// The test single molecule MZ only.
        /// </summary>
        [Test][STAThread]
        public void TestMixedSampleCrossSectionExtraction()
        {
            string fileLocation = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\mix\Mix1_8Oct13_Columbia_DI.uimf";
            IonizationMethod method = IonizationMethod.SodiumPlus;

            IList<IImsTarget> targetList = new List<IImsTarget>();
            targetList.Add(new PeptideTarget(12, "DGWHSWPIAHQWPQGPSAVDAAFSWEEK", 1.0));
            targetList.Add(new MolecularTarget("C3H7O7P", method, "chemical A"));
            targetList.Add(new MolecularTarget("C3H7O7P", method, "chemical B(A's isomer)"));
            targetList.Add(new MolecularTarget("C6H14O12P2", method, "chemical C"));
            targetList.Add(new MolecularTarget("C4H6O5", method, "chemical D"));
            targetList.Add(new MolecularTarget("C3H4O3", method, "chemical E"));
            targetList.Add(new MolecularTarget("C5H11O8P", method, "chemical F"));
            targetList.Add(new MolecularTarget("C6H8O7", method, "chemical G"));
            targetList.Add(new MolecularTarget("C4H6O4", method, "chemical H"));
            targetList.Add(new MolecularTarget("C3H5O6P", method, "chemical I"));
            targetList.Add(new MolecularTarget("C7H15O10P", method, "chemical J"));
            targetList.Add(new MolecularTarget(120.5, method, "Mz specified chemical L"));
            
            Console.WriteLine("Dataset: {0}", fileLocation);
            Console.WriteLine("TargetList: ");

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters(); 

            CrossSectionWorkfow informedWorkflow = new CrossSectionWorkfow(fileLocation, "output", parameters);
            IEnumerable<CrossSectionWorkflowResult> resultMap = informedWorkflow.RunCrossSectionWorkFlow(targetList, false);
            informedWorkflow.Dispose();
        }
        
        /// <summary>
        /// The test scoring.
        /// </summary>
        [Test][STAThread]
        public void TestMzmlExport()
        {
            VoltageAccumulationWorkflow workflow = new VoltageAccumulationWorkflow(true, F1E, "output");
            workflow.RunVoltageAccumulationWorkflow(FileFormatEnum.MzML);
            workflow.RunVoltageAccumulationWorkflow(FileFormatEnum.UIMF);
        }

        /// <summary>
        /// The test scoring.
        /// </summary>
        [Test][STAThread]
        public void TestScoring()
        {
            string formula = "C9H13ClN6";
            string fileLocation = Cae;
            MolecularTarget target = new MolecularTarget(formula, IonizationMethod.ProtonPlus, "CAE");
            
            Console.WriteLine("Dataset: {0}", fileLocation);
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
            VoltageSeparatedAccumulatedXiCs accumulatedXiCs = new VoltageSeparatedAccumulatedXiCs(uimfReader, target.MassWithAdduct, parameters.MassToleranceInPpm);

            Console.WriteLine();

            // For each voltage, find 2D XIC features 
            foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
            {
                Console.WriteLine("Voltage group: {0} V, [{1}-{2}]", voltageGroup.MeanVoltageInVolts, voltageGroup.FirstFrameNumber, voltageGroup.LastFrameNumber);

                List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;
                List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, smoother, parameters.FeatureFilterLevel);
                List<StandardImsPeak> standardPeaks = featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, uimfReader, voltageGroup, target.MassWithAdduct, parameters.MassToleranceInPpm)).ToList();

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
                    
                    double peakShapeScore = FeatureScoreUtilities.PeakShapeScore(featurePeak, workflow.uimfReader, workflow.Parameters.MassToleranceInPpm, workflow.Parameters.DriftTimeToleranceInMs, voltageGroup, globalMaxIntensity, workflow.NumberOfScans);
                    
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
        public void TestImportingDriftTimeLibrary()
        {
            IList<DriftTimeTarget> targets = DriftTimeLibraryImporter.ImportDriftTimeLibrary(AmtLibPos);
            Assert.AreEqual(targets.Count, 351);
        }
    }
}
