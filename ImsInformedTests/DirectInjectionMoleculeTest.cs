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
    using DeconTools.Backend.Workflows;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Interfaces;
    using ImsInformed.IO;
    using ImsInformed.Scoring;
    using ImsInformed.Stats;
    using ImsInformed.Targets;
    using ImsInformed.Util;
    using ImsInformed.Workflows.CrossSectionExtraction;
    using ImsInformed.Workflows.VoltageAccumulation;

    using InformedProteomics.Backend.Data.Composition;

    using MathNet.Numerics.Distributions;

    using MultiDimensionalPeakFinding;
    using MultiDimensionalPeakFinding.PeakDetection;

    using NUnit.Framework;

    using PNNLOmics.Data;

    using UIMFLibrary;

    using Ion = InformedProteomics.Backend.Data.Biology.Ion;
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
        public const string Bps = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-BPS_neg2_28Aug14_Columbia_DI.uimf";

        /// <summary>
        /// The cae.
        /// </summary>
        public const string Cae = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-CAE_pos2_9Oct14_Columbia_DI.uimf";

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

            // BPS Na
            string formula = "C12H10O4S";
            MolecularTarget sample = new MolecularTarget(formula, IonizationMethod.ProtonMinus);
            string fileLocation = Bps;

            Console.WriteLine("Dataset: {0}", fileLocation);

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            CrossSectionWorkfow workfow = new CrossSectionWorkfow(fileLocation, "output", "result.txt", parameters);
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
            // double mz = 249.02160599;
            // string uimfFile = DirectInjectionMoleculeTest.Bps;

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
            double mz = 221.059395;
            string uimfFile = AcetamipridFile;

            MolecularTarget target= new MolecularTarget(mz, IonizationMethod.ProtonMinus);
            Console.WriteLine("Nicotine:");
            Console.WriteLine("MZ:   " + mz);

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            CrossSectionWorkfow workflow = new CrossSectionWorkfow(uimfFile, "output", "result.txt", parameters);
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

            MolecularTarget target = new MolecularTarget(mz, IonizationMethod.ProtonMinus);

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            Assert.Throws<FileNotFoundException>(() => new CrossSectionWorkfow(uimfFile, "output", "result.txt", parameters));
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
            Assert.Throws<Exception>(() => new MolecularTarget(formula, IonizationMethod.ProtonMinus));
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
            Console.WriteLine("[Intensity], [Distance1], [Distance2], [Angle], [Pearson], [Bucha]");

            string fileLocation = Bps;
            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();
            CrossSectionWorkfow workflow = new CrossSectionWorkfow(fileLocation, "output", "result.txt", parameters);

            foreach (var form in formulas)
            {
                bool found = false;
                string formula = form;
                
                MolecularTarget target = new MolecularTarget(formula, new IonizationAdduct(IonizationMethod.ProtonMinus));
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
                VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(uimfReader, target.MassWithAdduct, parameters.MassToleranceInPpm);

                var voltageGroup = accumulatedXiCs.Keys.First();

                // Find peaks using multidimensional peak finder.
                List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;
                List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, smoother, parameters.FeatureFilterLevel);
                List<StandardImsPeak> standardPeaks = featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, uimfReader, voltageGroup, target.MassWithAdduct, parameters.MassToleranceInPpm)).ToList();

                // feature scorings and target selection.
                FeatureScoreHolder mostLikelyPeakScores;
                mostLikelyPeakScores.IntensityScore = 0;
                mostLikelyPeakScores.IsotopicScore = 0;
                mostLikelyPeakScores.PeakShapeScore = 0;
                double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, uimfReader);

                // Check each XIC Peak found
                foreach (var peak in standardPeaks)
                {
                    // Evaluate feature scores.
                    double intensityScore = FeatureScores.IntensityScore(peak, globalMaxIntensity);
                    
                    double isotopicScoreAngle = FeatureScores.IsotopicProfileScore(
                        peak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.Angle, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);

                    double isotopicScoreDistance = FeatureScores.IsotopicProfileScore(
                        peak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.EuclideanDistance, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);

                    double isotopicScorePerson = FeatureScores.IsotopicProfileScore(
                        peak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.PearsonCorrelation, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);

                    double isotopicScoreBhattacharyya = FeatureScores.IsotopicProfileScore(
                        peak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.Bhattacharyya, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);

                    double isotopicScoreDistanceAlternative = FeatureScores.IsotopicProfileScore(
                        peak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.EuclideanDistanceAlternative, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);
                    
                    double peakShapeScore = FeatureScores.PeakShapeScore(peak, workflow.uimfReader, workflow.Parameters.MassToleranceInPpm, workflow.Parameters.DriftTimeToleranceInMs, voltageGroup, globalMaxIntensity, workflow.NumberOfScans);
                    
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
                    Console.WriteLine("0");
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
        public void TestMixedSamples()
        {
            double targetK = 120.5;
            string targetL = "DGWHSWPIAHQWPQGPSAVDAAFSWEEK";
            string targetA = "C3H7O7P";
            string targetB = "C3H7O6P";
            string targetC = "C6H14O12P2";
            string targetD = "C4H6O5";
            string targetE = "C3H4O3";
            string targetF = "C5H11O8P";
            string targetG = "C6H8O7";
            string targetH = "C4H6O4";
            string targetI = "C3H5O6P";
            string targetJ = "C7H15O10P";
            
            string fileLocation = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\mix\Mix1_8Oct13_Columbia_DI.uimf";
            IonizationMethod method = IonizationMethod.SodiumPlus;

            IList<IImsTarget> targetList = new List<IImsTarget>();
            targetList.Add(new PeptideTarget(12, targetL, 1.0));
            targetList.Add(new MolecularTarget(targetA, method));
            targetList.Add(new MolecularTarget(targetB, method));
            targetList.Add(new MolecularTarget(targetC, method));
            targetList.Add(new MolecularTarget(targetD, method));
            targetList.Add(new MolecularTarget(targetE, method));
            targetList.Add(new MolecularTarget(targetF, method));
            targetList.Add(new MolecularTarget(targetG, method));
            targetList.Add(new MolecularTarget(targetH, method));
            targetList.Add(new MolecularTarget(targetI, method));
            targetList.Add(new MolecularTarget(targetJ, method));
            targetList.Add(new MolecularTarget(targetK, method));
            
            Console.WriteLine("Dataset: {0}", fileLocation);
            Console.WriteLine("TargetList: ");

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters(); 

            CrossSectionWorkfow informedWorkflow = new CrossSectionWorkfow(fileLocation, "output", "result.txt", parameters);
            IDictionary<string, CrossSectionWorkflowResult> resultMap = informedWorkflow.RunCrossSectionWorkFlow(targetList, false);
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
            MolecularTarget target = new MolecularTarget(formula, IonizationMethod.ProtonPlus);
            
            Console.WriteLine("Dataset: {0}", fileLocation);
            Console.WriteLine("CompositionWithoutAdduct: " + target.CompositionWithoutAdduct);
            Console.WriteLine("Monoisotopic MonoisotopicMass: " + target.MonoisotopicMass);

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            var smoother = new SavitzkyGolaySmoother(parameters.NumPointForSmoothing, 2);

            CrossSectionWorkfow workflow = new CrossSectionWorkfow(fileLocation, "output", "result.txt", parameters);

            Console.WriteLine("Ionization method: " + target.Adduct);
            Console.WriteLine("Targeting Mz: " + target.MassWithAdduct);
                
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
            VoltageSeparatedAccumulatedXICs accumulatedXiCs = new VoltageSeparatedAccumulatedXICs(uimfReader, target.MassWithAdduct, parameters.MassToleranceInPpm);

            Console.WriteLine();

            // For each voltage, find 2D XIC features 
            foreach (VoltageGroup voltageGroup in accumulatedXiCs.Keys)
            {
                Console.WriteLine("Voltage group: {0} V, [{1}-{2}]", voltageGroup.MeanVoltageInVolts, voltageGroup.FirstFrameNumber, voltageGroup.LastFrameNumber);

                List<IntensityPoint> intensityPoints = accumulatedXiCs[voltageGroup].IntensityPoints;
                List<FeatureBlob> featureBlobs = PeakFinding.FindPeakUsingWatershed(intensityPoints, smoother, parameters.FeatureFilterLevel);
                List<StandardImsPeak> standardPeaks = featureBlobs.Select(featureBlob => new StandardImsPeak(featureBlob, uimfReader, voltageGroup, target.MassWithAdduct, parameters.MassToleranceInPpm)).ToList();

                // feature scorings and target selection.
                FeatureScoreHolder mostLikelyPeakScores;
                mostLikelyPeakScores.IntensityScore = 0;
                mostLikelyPeakScores.IsotopicScore = 0;
                mostLikelyPeakScores.PeakShapeScore = 0;
                double globalMaxIntensity = MoleculeUtil.MaxDigitization(voltageGroup, uimfReader);

                // Check each XIC Peak found
                foreach (var featurePeak in standardPeaks)
                {
                    // Evaluate feature scores.
                   double intensityScore = FeatureScores.IntensityScore(featurePeak, globalMaxIntensity);
                    
                   double isotopicScoreAngle = FeatureScores.IsotopicProfileScore(
                        featurePeak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.Angle, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);

                    double isotopicScoreDistance = FeatureScores.IsotopicProfileScore(
                        featurePeak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.EuclideanDistance, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);

                    double isotopicScorePerson = FeatureScores.IsotopicProfileScore(
                        featurePeak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.PearsonCorrelation, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);

                    double isotopicScoreBhattacharyya = FeatureScores.IsotopicProfileScore(
                        featurePeak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.Bhattacharyya, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);

                    double isotopicScoreDistanceAlternative = FeatureScores.IsotopicProfileScore(
                        featurePeak, 
                        workflow.uimfReader, 
                        target, 
                        theoreticalIsotopicProfilePeakList, 
                        voltageGroup, 
                        IsotopicScoreMethod.EuclideanDistanceAlternative, 
                        globalMaxIntensity, 
                        workflow.NumberOfScans);
                    
                    double peakShapeScore = FeatureScores.PeakShapeScore(featurePeak, workflow.uimfReader, workflow.Parameters.MassToleranceInPpm, workflow.Parameters.DriftTimeToleranceInMs, voltageGroup, globalMaxIntensity, workflow.NumberOfScans);
                    
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
    }
}
