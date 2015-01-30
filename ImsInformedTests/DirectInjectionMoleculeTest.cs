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

    using DeconTools.Backend.Utilities;

    using ImsInformed.Domain;
    using ImsInformed.Parameters;
    using ImsInformed.Stats;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Composition;

    using MathNet.Numerics.Distributions;

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
            // Console.WriteLine("Nicotine:");
            // Console.WriteLine("Composition: " + sample.Composition);
            // Console.WriteLine("Monoisotopic Mass: " + sample.Mass);
            // Console.WriteLine("MZ:   " +  221.0594);
            // string fileLocation = AcetamipridFile;

            // Acetamiprid
            // string formula = "C10H11ClN4";
            // ImsTarget sample = new ImsTarget(1, IonizationMethod.ProtonMinus, formula);
            // Console.WriteLine("Acetamiprid:");
            // Console.WriteLine("Composition: " + sample.Composition);
            // Console.WriteLine("Monoisotopic Mass: " + sample.Mass);
            // Console.WriteLine("MZ:   " +  221.0594);
            // string fileLocation = AcetamipridFile;

            // BPS Na
            string formula = "C12H10O4S";
            ImsTarget sample = new ImsTarget(1, IonizationMethod.ProtonMinus, formula);
            string fileLocation = DirectInjectionMoleculeTest.Bps;
            Console.WriteLine("BPS:");
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
                IsotopicFitScoreMax = 0.15,
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
                IsotopicFitScoreMax = 0.15,
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

            result = NormalityTest.PeakNormalityTest(sampleTypical, normalityTestFunc, sampleSize);
            Console.WriteLine("sampleTypical: " + result);

            double[] sampleActualPeak = 
            { 
                 0.203, 0.382, 0.477, 0.48, 0.54, 0.62, 0.54, 0.48, 0.382, 0.203
            };

            result = NormalityTest.PeakNormalityTest(sampleActualPeak, normalityTestFunc, sampleSize);
            Console.WriteLine("sampleActualPeak: " + result);

            // Subjective shapes
            Console.WriteLine();
            Console.WriteLine("Peaks with subjective shapes");

             double[] sampleAll1s =
            { 
                 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 
            };

            result = NormalityTest.PeakNormalityTest(sampleAll1s, normalityTestFunc, sampleSize);
            Console.WriteLine("sampleAll1s: " + result);
            
            double[] sampleAll0s =
            { 
                 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };

            result = NormalityTest.PeakNormalityTest(sampleAll0s, normalityTestFunc, sampleSize);
            Console.WriteLine("sampleAll0s: " + result);

            double[] smallSample =
            { 
                 0, 1, 0
            };

            result = NormalityTest.PeakNormalityTest(smallSample, normalityTestFunc, sampleSize);
            Console.WriteLine("smallSample: " + result);

            // Bad shaped peaks
            Console.WriteLine();
            Console.WriteLine("Peaks with relatively bad shape");

            double[] doublePeak =
            { 
                  0.203, 0.382, 200, 1, 0.54, 200, 0, 0.48, 0.382, 0.203
            };

            result = NormalityTest.PeakNormalityTest(doublePeak, normalityTestFunc, sampleSize);
            Console.WriteLine("doublePeak: " + result);

            double[] kindaLikeNoise =
            { 
                 0.203, 0.503, 0.477, 0.621, 0.710, 200, 0.554, 0.329, 0.480, 0.382
            };

            result = NormalityTest.PeakNormalityTest(kindaLikeNoise, normalityTestFunc, sampleSize);
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
    }
}
