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
namespace ImsInformedTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using DeconTools.Backend.Utilities;

    using ImsInformed;
    using ImsInformed.IO;
    using ImsInformed.Targets;
    using ImsInformed.Workflows.CrossSectionExtraction;
    using ImsInformed.Workflows.DriftTimeLibraryMatch;
    using ImsInformed.Workflows.VoltageAccumulation;

    using InformedProteomics.Backend.Data.Composition;

    using NUnit.Framework;

    using PNNLOmics.Data;

    /// <summary>
    /// The direct injection molecule test.
    /// </summary>
    public class DirectInjectionMoleculeTest
    {
        /// <summary>
        /// The nicotine UIMF file.
        /// </summary>
        public const string NicoFile = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-NIC_neg2_28Aug14_Columbia_DI.uimf";

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
        public const string BHC = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-BHC_pos2_13Sep14_Columbia_DI.uimf";

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
        /// The amt lib pos.
        /// </summary>
        public const string AmtLibPos = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\example_library_files\viper_pos_chemical_based.txt";

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
        /// The test single molecule with formula.
        /// </summary>
        [Test][STAThread]
        public void TestSingleMoleculeWithFormula()
        {
            // Nicotine
            // string formula = "C10H14N2";
            // ImsTarget sample = new ImsTarget(1, Adduct.Protonated, formula);
            // Console.WriteLine("MZ:   " +  221.0594);
            // string fileLocation = AcetamipridFile;

            // Acetamiprid
            // string formula = "C10H11ClN4";
            // ImsTarget sample = new ImsTarget(1, Adduct.Deprotonated, formula);
            // string fileLocation = AcetamipridFile;

            // F1E
            // string formula = "C15H21Cl2FN2O3";
            // MolecularTarget sample = new MolecularTarget(formula, IonizationMethod.Protonated);
            // string fileLocation = F1E;

            // BPS Negative
            // string formula = "C12H10O4S";
            // MolecularTarget sample = new MolecularTarget(formula, IonizationMethod.prot);
            // string fileLocation = BPSNegative;

            // BPS Positive
            string formula = "C12H10O4S";
            MolecularTarget sample = new MolecularTarget(formula, IonizationMethod.Protonated, "BPS");
            string fileLocation = BPSPostive;

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            CrossSectionWorkfow workfow = new CrossSectionWorkfow(fileLocation, "output", parameters);
            CrossSectionWorkflowResult result = workfow.RunCrossSectionWorkFlow(sample, true);
            Assert.AreEqual(1, result.IdentifiedIsomers.Count());
            Assert.LessOrEqual(Math.Abs(result.IdentifiedIsomers.First().CrossSectionalArea - 166.3012), 0.5);
            workfow.Dispose();
        }

        /// <summary>
        /// The test target detection with isomers.
        /// </summary>
        [Test][STAThread]
        [TestCase("C5H14NO", IonizationMethod.APCI, "choline", @"Z:\choline\20151113_choline0001.uimf", Result=1)]
        [TestCase("C5H14NO", IonizationMethod.APCI, "choline", @"Z:\choline\20151113_choline0002.uimf", Result=1)]
        [TestCase("C5H14NO", IonizationMethod.APCI, "choline", @"Z:\choline\20151113_choline0003.uimf", Result=1)]
        [TestCase("C5H14NO", IonizationMethod.APCI, "choline", @"Z:\choline\20151113_choline0004.uimf", Result=1)]
        [TestCase("C5H14NO", IonizationMethod.APCI, "choline", @"Z:\choline\20151113_choline0005.uimf", Result=1)]
        [TestCase("C10H12N3O3PS2", IonizationMethod.Sodiumated, "AZY+Na", @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-AZY_pos2_9Oct14_Columbia_DI.uimf", Result=2)]
        [TestCase("C12H6F2N2O2",IonizationMethod.Sodiumated, "FIL+Na",  @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-FIL_pos_10Oct14_Columbia_DI.uimf", Result = 2)]
        [TestCase("C18H12Cl2N2O", IonizationMethod.Deprotonated, "BAD-H", @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-BAD_neg_13Oct14_Columbia_DI.uimf", Result = 1)]
        [TestCase("C18H12Cl2N2O", IonizationMethod.Sodiumated, "BAD+Na", @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-BAD_pos_10Oct14_Columbia_DI.uimf", Result = 2)]
        [TestCase("C18H12Cl2N2O", IonizationMethod.Protonated, "BAD+H", @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-BAD_pos_10Oct14_Columbia_DI.uimf", Result = 1)]
        [TestCase("C12H18N4O6S", IonizationMethod.Protonated, "OYZ+H", @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-OYZ_pos_12Sep14_Columbia_DI.uimf", Result = 1)]
        [TestCase("C12H18N4O6S", IonizationMethod.Sodiumated, "OYZ+Na", @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-OYZ_pos_12Sep14_Columbia_DI.uimf", Result = 1)]
        [TestCase("C12H18N4O6S", IonizationMethod.Deprotonated, "OYZ-H", @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\smallMolecule\EXP-OYZ_neg_26Aug14_Columbia_DI.uimf", Result = 1)]
        public int TestTargetDetectionWithIsomersClean(string formula, IonizationMethod method, string descriptor, string fileLocation)
        {
            MolecularTarget sample1 = new MolecularTarget(formula, method, descriptor);
            
            CrossSectionSearchParameters parameters1 = new CrossSectionSearchParameters();

            CrossSectionWorkfow workfow1 = new CrossSectionWorkfow(fileLocation, "output", parameters1);
            CrossSectionWorkflowResult results1 = workfow1.RunCrossSectionWorkFlow(sample1, true);
            workfow1.Dispose();
            return results1.IdentifiedIsomers.Count();
        }

        /// <summary>
        /// The test target detection with isomers.
        /// </summary>
        [Test][STAThread]
        public void TestTargetDetectionWithIsomersHard()
        {
            // BHC
            string formula3 = "C13H18ClNO";
            MolecularTarget sample3 = new MolecularTarget(formula3, IonizationMethod.Protonated, "BHC");
            string fileLocation3 = BHC;
            
            CrossSectionSearchParameters parameters3 = new CrossSectionSearchParameters();
            
            CrossSectionWorkfow workfow3 = new CrossSectionWorkfow(fileLocation3, "output", parameters3);
            CrossSectionWorkflowResult results3 = workfow3.RunCrossSectionWorkFlow(sample3, true);
            Assert.AreEqual(2, results3.IdentifiedIsomers.Count());
            workfow3.Dispose();
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

            MolecularTarget target = new MolecularTarget(mz, IonizationMethod.Protonated, "Nicotine");

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            CrossSectionWorkfow workflow = new CrossSectionWorkfow(uimfFile, "output",  parameters);
            workflow.RunCrossSectionWorkFlow(target);
            workflow.Dispose();
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

            MolecularTarget target = new MolecularTarget(mz, IonizationMethod.Deprotonated, "BPS");

            CrossSectionSearchParameters parameters = new CrossSectionSearchParameters();

            Assert.Throws<FileNotFoundException>(() => new CrossSectionWorkfow(uimfFile, "output", parameters));
        }

         /// <summary>
        /// The test single molecule MZ only.
        /// </summary>
        [Test][STAThread]
        public void TestMixedSampleLibraryMatching()
        {
            // TODO Import AMT library instead of manually add them
            IList<DriftTimeTarget> imsTargets = new List<DriftTimeTarget>();
            DriftTimeTarget t1 = new DriftTimeTarget("BPS protonated", 23.22, "C12H10O4S", IonizationMethod.Protonated);
            DriftTimeTarget t2 = new DriftTimeTarget("BPS sodiated", 31.8506, "C12H10O4S", IonizationMethod.Sodiumated);
            DriftTimeTarget t3 = new DriftTimeTarget("I made it up", 15, "C14H14O4S", IonizationMethod.Sodiumated);
            DriftTimeTarget t4 = new DriftTimeTarget("I made it up again", 15, "C12H11O4S", IonizationMethod.Sodiumated);
            
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
            IonizationMethod method = IonizationMethod.Sodiumated;

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
            targetList.Add(new MolecularTarget(120.5, method, "MzInDalton specified chemical L"));
            
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
        /// The test single molecule MZ only.
        /// </summary>
        [Test][STAThread]
        public void TestSingleMoleculeBadTarget()
        {
            string formula = "NotAFormula";
            Assert.Throws<Exception>(() => new MolecularTarget(formula, IonizationMethod.Deprotonated, "Magic molecule"));
        }

        [Test][STAThread]
        public void TestImportingDriftTimeLibrary()
        {
            IList<DriftTimeTarget> targets = DriftTimeLibraryImporter.ImportDriftTimeLibrary(AmtLibPos);
            Assert.AreEqual(targets.Count, 351);
        }
    }
}
