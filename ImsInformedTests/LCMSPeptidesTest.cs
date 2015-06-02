// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FunctionalTests.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2015 Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the FunctionalTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ImsInformedTests
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.IO;
    using System.Linq;

    using DeconTools.Backend.Algorithms;

    using ImsInformed.Domain;
    using ImsInformed.IO;
    using ImsInformed.Targets;
    using ImsInformed.Util;
    using ImsInformed.Workflows.LcImsPeptideExtraction;

    using InformedProteomics.Backend.Data.Sequence;

    using MathNet.Numerics.Interpolation;

    using NUnit.Framework;

    using UIMFLibrary;

    /// <summary>
    /// The functional tests.
    /// </summary>
    public class FunctionalTests
    {
        /// <summary>
        /// The cheetah UIMF file.
        /// </summary>
        public const string Cheetah = @"\\proto-2\UnitTest_Files\IMSInformedTestFiles\datasets\peptide\Sarc_MS2_90_6Apr11_Cheetah_11-02-19.uimf";

        /// <summary>
        /// The test single peptide.
        /// </summary>
        [Test]
        public void TestSinglePeptide()
        {
            string uimfFileLocation = Cheetah;

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.1,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                ScanWindowWidth = 4,
                NumPointForSmoothing = 9
            };

            string peptide = "DGWHSWPIAHQWPQGPSAVDAAFSWEEK";
            double net = 0.4832;

            PeptideTarget target = new PeptideTarget(1, peptide, net);

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters);
            lcImsPeptideSearchWorkfow.RunInformedWorkflow(target);
        }

        /// <summary>
        /// The test single peptide 2.
        /// </summary>
        [Test]
        public void TestSinglePeptide2()
        {
            string uimfFileLocation = Cheetah;

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.2,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            string peptide = "ATVLNYLPK";
            double net = 0.3612;

            PeptideTarget target = new PeptideTarget(1, peptide, net);
            DriftTimeTarget driftTimeTarget = new DriftTimeTarget("Test Target", 19.62, target, 2);
            target.DriftTimeTargetList.Add(driftTimeTarget);

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters);
            ChargeStateCorrelationResult correlationResult = lcImsPeptideSearchWorkfow.RunInformedWorkflow(target);

            using (ImsTargetResultExporter resultsExporter = new ImsTargetResultExporter("outputSingle.csv"))
            {
                if (correlationResult != null) resultsExporter.AppendCorrelationResultToCsv(correlationResult);
            }
        }

        /// <summary>
        /// The test very saturated peptide.
        /// </summary>
        [Test]
        public void TestVerySaturatedPeptide()
        {
            string uimfFileLocation = Cheetah;

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 2,
                NetTolerance = 0.03,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            string peptide = "NFPSPVDAAFR";
            double net = 0.45;

            PeptideTarget target = new PeptideTarget(1, peptide, net);

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters);
            lcImsPeptideSearchWorkfow.RunInformedWorkflow(target);
        }

        /// <summary>
        /// The test consistent peptide.
        /// </summary>
        [Test]
        public void TestConsistentPeptide()
        {
            string uimfFileLocation = Cheetah;

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 2,
                NetTolerance = 0.1,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            string peptide = "IMQSSQSMSK";
            double net = 0.096;

            PeptideTarget target = new PeptideTarget(1, peptide, net);

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters);
            lcImsPeptideSearchWorkfow.RunInformedWorkflow(target);
        }

        /// <summary>
        /// The test saturated peptide.
        /// </summary>
        [Test]
        public void TestSaturatedPeptide()
        {
            string uimfFileLocation = Cheetah;

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.1,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            string peptide = "QGHNSVFLIKGDK";
            double net = 0.2493;

            PeptideTarget target = new PeptideTarget(1, peptide, net);

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters);
            lcImsPeptideSearchWorkfow.RunInformedWorkflow(target);
        }

        /// <summary>
        /// The test import targets.
        /// </summary>
        [Test]
        public void TestImportTargets()
        {
            List<PeptideTarget> targetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789");
            Console.WriteLine(targetList.Count);
        }

        /// <summary>
        /// The test run all targets.
        /// </summary>
        [Test]
        public void TestRunAllTargets()
        {
            string uimfFileLocation = Cheetah;
            string netAlignmentFileLocation = @"";

            IInterpolation interpolation = AlignmentImporter.ReadFile(netAlignmentFileLocation);

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.03,
                IsotopicFitScoreThreshold = 0.2,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            List<PeptideTarget> targetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789");

            Console.WriteLine(DateTime.Now + ": Using " + targetList.Count + " targets.");

            using (ImsTargetResultExporter allResultsExporter = new ImsTargetResultExporter("outputTestAll.csv"))
            {
                using (ImsTargetResultExporter resultsExporter = new ImsTargetResultExporter("outputTest.csv"))
                {
                    LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters, interpolation);

                    foreach (var imsTarget in targetList)
                    {
                        ChargeStateCorrelationResult correlationResult = lcImsPeptideSearchWorkfow.RunInformedWorkflow(imsTarget);

                        allResultsExporter.AppendResultsOfTargetToCsv(imsTarget);

                        if (correlationResult == null) continue;

                        resultsExporter.AppendCorrelationResultToCsv(correlationResult);
                    }
                }
            }
        }

        /// <summary>
        /// The test run all targets and calibrate.
        /// </summary>
        [Test]
        public void TestRunAllTargetsAndCalibrate()
        {
            // Setup sqlite output file by deleting any current file and copying a blank schema over
            string sqliteSchemaLocation = @"..\..\..\testFiles\informedSchema.db3";
            string sqliteOutputLocation = "Sarc_Many_Datasets_All_Aligned.db3";
            if (File.Exists(sqliteOutputLocation)) File.Delete(sqliteOutputLocation);
            File.Copy(sqliteSchemaLocation, sqliteOutputLocation);

            // Setup calibration workflow and targets
            LcImsPeptideSearchParameters calibrationParameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.5,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            List<PeptideTarget> calibrationTargetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789", 1e-10, true);
            Console.WriteLine("Using " + calibrationTargetList.Count + " targets for calibration.");

            // Setup Informed workflow parameters
            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.03,
                IsotopicFitScoreThreshold = 0.2,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            Console.WriteLine(DateTime.Now + ": Reading ViperCompatibleMass Tags from MTDB");
            List<PeptideTarget> targetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789");

            Console.WriteLine(DateTime.Now + ": Using " + targetList.Count + " targets.");

            // Put all UIMF Files to process here
            List<string> uimfFileList = new List<string>();
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_13_1Apr11_Cheetah_11-02-24.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_27_2Apr11_Cheetah_11-01-11.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_54_6Apr11_Cheetah_11-02-18.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_55_3Apr11_Cheetah_11-02-15.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_73_5Apr11_Cheetah_11-02-24.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_90_6Apr11_Cheetah_11-02-19.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_102_7Apr11_Cheetah_11-02-19.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_119_8Apr11_Cheetah_11-02-18.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_125_8Apr11_Cheetah_11-02-15.uimf");
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_146_9Apr11_Cheetah_11-02-24.uimf");

            using (var connection = new SQLiteConnection(@"Data Source=" + sqliteOutputLocation + ";New=False;"))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        Console.WriteLine(DateTime.Now + ": Writing ViperCompatibleMass Tags to database");

                        foreach (var target in targetList)
                        {
                            string insertMassTagsQuery = target.CreateSqlMassTagQueries();
                            command.CommandText = insertMassTagsQuery;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }

                    // Iterate over each UIMF File
                    for (int i = 0; i < uimfFileList.Count; i++)
                    {
                        string uimfFileLocation = uimfFileList[i];
                        FileInfo uimfFileInfo = new FileInfo(uimfFileLocation);
                        Console.WriteLine(DateTime.Now + ": Processing " + uimfFileInfo.Name);

                        // NET Alignment
                        string netAlignmentFileName = uimfFileInfo.Name.Replace(".uimf", "_NetAlign.csv");
                        string netAlignmentLocation = Path.Combine(uimfFileInfo.DirectoryName, netAlignmentFileName);
                        FileInfo netAlignmentFileInfo = new FileInfo(netAlignmentLocation);
                        if(!File.Exists(netAlignmentFileInfo.FullName))
                        {
                            Console.WriteLine(DateTime.Now + ": Creating alignment file using " + calibrationTargetList.Count + " possible targets.");
                            LcImsPeptideSearchWorkfow calibrationWorkflow = new LcImsPeptideSearchWorkfow(uimfFileLocation, calibrationParameters);
                            List<Tuple<double, double>> netAlignmentInput = new List<Tuple<double, double>>();

                            int index = 0;
                            // Run calibration workflow on each of the calibration targets
                            foreach (var imsTarget in calibrationTargetList.OrderBy(x => x.NormalizedElutionTime))
                            {
                                //Console.WriteLine(DateTime.Now + ": Processing Target " + index);
                                ChargeStateCorrelationResult correlationResult = calibrationWorkflow.RunInformedWorkflow(imsTarget);

                                if (correlationResult != null && correlationResult.CorrelatedResults.Any())
                                {
                                    var elutionTimeFilteredResults = correlationResult.CorrelatedResults.Where(x => x.NormalizedElutionTime >= 0.1);
                                    if(elutionTimeFilteredResults.Any())
                                    {
                                        LcImsTargetResult result = correlationResult.CorrelatedResults.Where(x => x.NormalizedElutionTime >= 0.1).OrderByDescending(x => x.Intensity).First();
                                        netAlignmentInput.Add(new Tuple<double, double>(result.NormalizedElutionTime, imsTarget.NormalizedElutionTime));
                                    }
                                }

                                //Console.WriteLine(DateTime.Now + ": Done Processing Target " + index);
                                imsTarget.RemoveResults();
                                //Console.WriteLine(DateTime.Now + ": Removed results from Target " + index);

                                index++;
                            }

                            // Place data points at beginning and end to finish off the alignment
                            netAlignmentInput.Add(new Tuple<double, double>(0, 0));
                            netAlignmentInput.Add(new Tuple<double, double>(1, 1));

                            // Do LOESS to get NET alignment
                            Console.WriteLine(DateTime.Now + ": Found " + netAlignmentInput.Count + " targets to use for alignment.");
                            var netAlignmentInputGroup = netAlignmentInput.GroupBy(x => x.Item1).OrderBy(x => x.Key);
                            var groupedNetTuple = netAlignmentInputGroup.Select(x => x.OrderBy(y => Math.Abs(y.Item1 - y.Item2)).First()).ToArray();
                            var loessInterpolatorForNetAlignment = new LoessInterpolator(0.1, 4);
                            double[] xArray = groupedNetTuple.Select(x => x.Item1).ToArray();
                            double[] yArray = groupedNetTuple.Select(x => x.Item2).ToArray();
                            double[] newNetValues = loessInterpolatorForNetAlignment.Smooth(xArray, yArray);

                            // Creates a file for the NET Alignment to be stored
                            using (StreamWriter writer = new StreamWriter(netAlignmentFileInfo.FullName))
                            {
                                for (int j = 0; j < groupedNetTuple.Length; j++)
                                {
                                    writer.WriteLine(groupedNetTuple[j].Item1 + "," + newNetValues[j]);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine(DateTime.Now + ": Using existing alignment file");
                        }

                        // Grab the net alignment
                        IInterpolation interpolation = AlignmentImporter.ReadFile(netAlignmentFileInfo.FullName);

                        LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileInfo.FullName, parameters, interpolation);

                        // Insert Dataset Info
                        using (var transaction = connection.BeginTransaction())
                        {
                            string insertDatasetQuery = "INSERT INTO T_Dataset (Dataset_Id, File_Name) VALUES(" + i + ",'" + uimfFileInfo.Name + "');";
                            command.CommandText = insertDatasetQuery;
                            command.ExecuteNonQuery();
                            transaction.Commit();
                        }

                        Console.WriteLine(DateTime.Now + ": Processing targets");

                        // Execute workflow on each Target and write results to database
                        foreach (var imsTarget in targetList)
                        {
                            using (var transaction = connection.BeginTransaction())
                            {
                                ChargeStateCorrelationResult correlationResult = lcImsPeptideSearchWorkfow.RunInformedWorkflow(imsTarget);

                                string queries = imsTarget.CreateSqlResultQueries(i);
                                command.CommandText = queries;
                                command.ExecuteNonQuery();

                                if (correlationResult != null && correlationResult.CorrelationMap.Count > 1)
                                {
                                    string correlationQueries = correlationResult.CreateSqlUpdateQueries();
                                    if (correlationQueries != "")
                                    {
                                        command.CommandText = correlationQueries;
                                        command.ExecuteNonQuery();
                                    }
                                }

                                transaction.Commit();
                            }

                            // Reset the Target so it can be used again by another dataset
                            imsTarget.RemoveResults();
                        }
                    }
                }

                connection.Close();
            }
        }

        /// <summary>
        /// The test run all targets SQL output.
        /// </summary>
        [Test]
        public void TestRunAllTargetsSqlOutput()
        {
            // Setup sqlite output file by deleting any current file and copying a blank schema over
            string sqliteSchemaLocation = @"..\..\..\testFiles\informedSchema.db3";
            string sqliteOutputLocation = "Sarc_Many_Datasets.db3";
            if(File.Exists(sqliteOutputLocation)) File.Delete(sqliteOutputLocation);
            File.Copy(sqliteSchemaLocation, sqliteOutputLocation);

            //string uimfFileLocation = @"..\..\..\testFiles\Sarc_MS2_90_6Apr11_Cheetah_11-02-19.uimf";
            //string uimfFileLocation = @"..\..\..\testFiles\Sarc_P23_C07_2143_23Feb12_Cheetah_11-05-40.uimf";

            //IInterpolation interpolation = AlignmentImporter.ReadFile(netAlignmentFileLocation);

            // Setup calibration workflow and targets
            LcImsPeptideSearchParameters calibrationParameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.5,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            List<PeptideTarget> calibrationTargetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789", 1e-10, true);
            Console.WriteLine("Using " + calibrationTargetList.Count + " targets for calibration.");

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.03,
                IsotopicFitScoreThreshold = 0.2,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            List<PeptideTarget> targetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789");
            Console.WriteLine(DateTime.Now + ": Reading ViperCompatibleMass Tags from MTDB");

            Console.WriteLine(DateTime.Now + ": Using " + targetList.Count + " targets.");

            List<string> uimfFileList = new List<string>();
            uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_13_1Apr11_Cheetah_11-02-24.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_27_2Apr11_Cheetah_11-01-11.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_54_6Apr11_Cheetah_11-02-18.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_55_3Apr11_Cheetah_11-02-15.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_73_5Apr11_Cheetah_11-02-24.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_90_6Apr11_Cheetah_11-02-19.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_102_7Apr11_Cheetah_11-02-19.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_119_8Apr11_Cheetah_11-02-18.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_125_8Apr11_Cheetah_11-02-15.uimf");
            //uimfFileList.Add(@"..\..\..\testFiles\Sarc_MS2_146_9Apr11_Cheetah_11-02-24.uimf");

            using (var connection = new SQLiteConnection(@"Data Source=" + sqliteOutputLocation + ";New=False;"))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        Console.WriteLine(DateTime.Now + ": Writing ViperCompatibleMass Tags to database");

                        //foreach (var Target in targetList)
                        //{
                        //    string insertMassTagsQuery = Target.CreateSqlMassTagQueries();
                        //    command.CommandText = insertMassTagsQuery;
                        //    command.ExecuteNonQuery();
                        //}

                        //transaction.Commit();
                    }

                    for (int i = 0; i < uimfFileList.Count; i++)
                    {
                        string uimfFileLocation = uimfFileList[i];
                        FileInfo uimfFileInfo = new FileInfo(uimfFileLocation);
                        Console.WriteLine(DateTime.Now + ": Processing " + uimfFileInfo.Name);

                        // NET Alignment
                        string netAlignmentFileName = uimfFileInfo.Name.Replace(".uimf", "_NetAlign.csv");
                        string netAlignmentLocation = Path.Combine(uimfFileInfo.DirectoryName, netAlignmentFileName);
                        FileInfo netAlignmentFileInfo = new FileInfo(netAlignmentLocation);
                        if (!File.Exists(netAlignmentFileInfo.FullName))
                        {
                            Console.WriteLine(DateTime.Now + ": Creating alignment file using " + calibrationTargetList.Count + " possible targets.");
                            LcImsPeptideSearchWorkfow calibrationWorkflow = new LcImsPeptideSearchWorkfow(uimfFileLocation, calibrationParameters);
                            List<Tuple<double, double>> netAlignmentInput = new List<Tuple<double, double>>();

                            int index = 0;
                            // Run calibration workflow on each of the calibration targets
                            foreach (var imsTarget in calibrationTargetList.OrderBy(x => x.NormalizedElutionTime))
                            {
                                //Console.WriteLine(DateTime.Now + ": Processing Target " + index);
                                ChargeStateCorrelationResult correlationResult = calibrationWorkflow.RunInformedWorkflow(imsTarget);

                                if (correlationResult != null && correlationResult.CorrelatedResults.Any())
                                {
                                    var elutionTimeFilteredResults = correlationResult.CorrelatedResults.Where(x => x.NormalizedElutionTime >= 0.1);
                                    if (elutionTimeFilteredResults.Any())
                                    {
                                        LcImsTargetResult result = correlationResult.CorrelatedResults.Where(x => x.NormalizedElutionTime >= 0.1).OrderByDescending(x => x.Intensity).First();
                                        netAlignmentInput.Add(new Tuple<double, double>(result.NormalizedElutionTime, imsTarget.NormalizedElutionTime));
                                    }
                                }

                                //Console.WriteLine(DateTime.Now + ": Done Processing Target " + index);
                                imsTarget.RemoveResults();
                                //Console.WriteLine(DateTime.Now + ": Removed results from Target " + index);

                                index++;
                            }

                            // Place data points at beginning and end to finish off the alignment
                            netAlignmentInput.Add(new Tuple<double, double>(0, 0));
                            netAlignmentInput.Add(new Tuple<double, double>(1, 1));

                            // Do LOESS to get NET alignment
                            Console.WriteLine(DateTime.Now + ": Found " + netAlignmentInput.Count + " targets to use for alignment.");
                            var netAlignmentInputGroup = netAlignmentInput.GroupBy(x => x.Item1).OrderBy(x => x.Key);
                            var groupedNetTuple = netAlignmentInputGroup.Select(x => x.OrderBy(y => Math.Abs(y.Item1 - y.Item2)).First()).ToArray();
                            var loessInterpolatorForNetAlignment = new LoessInterpolator(0.1, 4);
                            double[] xArray = groupedNetTuple.Select(x => x.Item1).ToArray();
                            double[] yArray = groupedNetTuple.Select(x => x.Item2).ToArray();
                            double[] newNetValues = loessInterpolatorForNetAlignment.Smooth(xArray, yArray);

                            // Creates a file for the NET Alignment to be stored
                            using (StreamWriter writer = new StreamWriter(netAlignmentFileInfo.FullName))
                            {
                                for (int j = 0; j < groupedNetTuple.Length; j++)
                                {
                                    writer.WriteLine(groupedNetTuple[j].Item1 + "," + newNetValues[j]);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine(DateTime.Now + ": Using existing alignment file");
                        }

                        // Grab the net alignment
                        IInterpolation interpolation = AlignmentImporter.ReadFile(netAlignmentFileInfo.FullName);

                        LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileInfo.FullName, parameters, interpolation);

                        //using (var transaction = connection.BeginTransaction())
                        //{
                        //    string insertDatasetQuery = "INSERT INTO T_Dataset (Dataset_Id, File_Name) VALUES(" + i + ",'" + uimfFileInfo.Name + "');";
                        //    command.CommandText = insertDatasetQuery;
                        //    command.ExecuteNonQuery();
                        //    transaction.Commit();
                        //}

                        List<ChargeStateCorrelationResult> resultList = new List<ChargeStateCorrelationResult>();

                        foreach (var imsTarget in targetList)
                        {
                            using (var transaction = connection.BeginTransaction())
                            {
                                ChargeStateCorrelationResult correlationResult = lcImsPeptideSearchWorkfow.RunInformedWorkflow(imsTarget);

                                if (correlationResult != null)
                                {
                                    resultList.Add(correlationResult);
                                }

                                //string queries = imsTarget.CreateSqlResultQueries(i);
                                //command.CommandText = queries;
                                //command.ExecuteNonQuery();

                                //if (correlationResult != null && correlationResult.CorrelationMap.Count > 1)
                                //{
                                //    string correlationQueries = correlationResult.CreateSqlUpdateQueries();
                                //    if (correlationQueries != "")
                                //    {
                                //        command.CommandText = correlationQueries;
                                //        command.ExecuteNonQuery();
                                //    }
                                //}

                                //transaction.Commit();
                            }

                            // Reset the Target so it can be used again by another dataset
                            imsTarget.RemoveResults();
                        }

                        foreach (var chargeStateCorrelationResult in resultList)
                        {
                            
                        }
                    }
                }

                connection.Close();
            }
        }

        /// <summary>
        /// The test calibration.
        /// </summary>
        [Test]
        public void TestCalibration()
        {
            string uimfFileLocation = Cheetah;

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.5,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            List<PeptideTarget> targetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789", 1e-10, true);
            Console.WriteLine("Using " + targetList.Count + " targets for calibration.");

            List<Tuple<double, double>> netAlignmentInput = new List<Tuple<double, double>>();
            List<Tuple<double, double>> massAlignmentInput = new List<Tuple<double, double>>();

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters);

            foreach (var imsTarget in targetList.OrderBy(x => x.NormalizedElutionTime))
            {
                ChargeStateCorrelationResult correlationResult = lcImsPeptideSearchWorkfow.RunInformedWorkflow(imsTarget);

                if (correlationResult == null || !correlationResult.CorrelatedResults.Any()) continue;

                LcImsTargetResult result = correlationResult.CorrelatedResults.OrderByDescending(x => x.Intensity).First();
                //LcImsTargetResult result = correlationResult.CorrelatedResults.OrderByDescending(X => X.Intensity * (1 - Math.Abs(X.NormalizedElutionTime - imsTarget.NormalizedElutionTime))).First();
                //LcImsTargetResult result = correlationResult.CorrelatedResults.OrderBy(X => X.NormalizedElutionTime).First();

                //if (netAlignmentInput.Count == 0 || Math.Abs(netAlignmentInput.Last().Item1 - imsTarget.NormalizedElutionTime) > 0.0001)
                //{
                //    netAlignmentInput.Add(new Tuple<double, double>(imsTarget.NormalizedElutionTime, result.NormalizedElutionTime));
                //    massAlignmentInput.Add(new Tuple<double, double>(imsTarget.NormalizedElutionTime, result.PpmError));
                //}

                netAlignmentInput.Add(new Tuple<double, double>(result.NormalizedElutionTime, imsTarget.NormalizedElutionTime));
                massAlignmentInput.Add(new Tuple<double, double>(result.IsotopicProfile.MonoPeakMZ, result.PpmError));
            }

            var netAlignmentInputGroup = netAlignmentInput.GroupBy(x => x.Item1).OrderBy(x => x.Key);
            var massAlignmentInputGroup = massAlignmentInput.GroupBy(x => x.Item1).OrderBy(x => x.Key);

            netAlignmentInput = netAlignmentInput.OrderBy(x => x.Item1).ToList();
            massAlignmentInput = massAlignmentInput.OrderBy(x => x.Item1).ToList();

            var groupedNetTuple = netAlignmentInputGroup.Select(x => x.OrderBy(y => Math.Abs(y.Item1 - y.Item2)).First()).ToArray();
            //var groupedNetTuple = netAlignmentInputGroup.Select(X => X.Average(Y => Y.Item2)).ToArray();
            var groupedMassTuple = massAlignmentInputGroup.Select(x => x.First()).ToArray();

            var loessInterpolatorForNetAlignment = new LoessInterpolator(0.1, 4);
            var loessInterpolatorForMassAlignment = new LoessInterpolator(0.2, 1);

            //double[] newNetValues = loessInterpolatorForNetAlignment.Smooth(netAlignmentInputGroup.Select(X => X.Key).ToArray(), netAlignmentInputGroup.Select(X => X.Average(Y => Y.Item2)).ToArray());
            double[] newNetValues = loessInterpolatorForNetAlignment.Smooth(groupedNetTuple.Select(x => x.Item1).ToArray(), groupedNetTuple.Select(x => x.Item2).ToArray());
            double[] newMassValues = loessInterpolatorForMassAlignment.Smooth(groupedMassTuple.Select(x => x.Item1).ToArray(), groupedMassTuple.Select(x => x.Item2).ToArray());

            using(StreamWriter writer = new StreamWriter("oldNetValues.csv"))
            {
                foreach (var netTuple in groupedNetTuple)
                {
                    writer.WriteLine(netTuple.Item1 + "," + netTuple.Item2);
                }
            }

            using (StreamWriter writer = new StreamWriter("oldMassValues.csv"))
            {
                foreach (var massTuple in groupedMassTuple)
                {
                    writer.WriteLine(massTuple.Item1 + "," + massTuple.Item2);
                }
            }

            using (StreamWriter writer = new StreamWriter("smoothedNetValues.csv"))
            {
                for (int i = 0; i < groupedNetTuple.Length; i++)
                {
                    writer.WriteLine(groupedNetTuple[i].Item1 + "," + newNetValues[i]);
                }
            }

            using (StreamWriter writer = new StreamWriter("smoothedMassValues.csv"))
            {
                for (int i = 0; i < groupedMassTuple.Length; i++)
                {
                    writer.WriteLine(groupedMassTuple[i].Item1 + "," + newMassValues[i]);
                }
            }
        }

        /// <summary>
        /// The test create new calibration from old calibration.
        /// </summary>
        [Test]
        public void TestCreateNewCalibrationFromOldCalibration()
        {
            FileInfo fileInfo = new FileInfo("oldNetValues.csv");

            List<double> xValues = new List<double>();
            List<double> yValues = new List<double>();

            xValues.Add(0);
            yValues.Add(0);

            using (TextReader reader = new StreamReader(fileInfo.FullName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] splitLine = line.Split(',');

                    double xValue = double.Parse(splitLine[0]);
                    double yValue = double.Parse(splitLine[1]);

                    xValues.Add(xValue);
                    yValues.Add(yValue);
                }
            }

            xValues.Add(1);
            yValues.Add(1);

            var loessInterpolatorForNetAlignment = new LoessInterpolator(0.1, 4);

            double[] newNetValues = loessInterpolatorForNetAlignment.Smooth(xValues.ToArray(), yValues.ToArray());

            using (StreamWriter writer = new StreamWriter("smoothedNetValues.csv"))
            {
                for (int i = 0; i < xValues.Count; i++)
                {
                    writer.WriteLine(xValues[i] + "," + newNetValues[i]);
                }
            }
        }

        /// <summary>
        /// The test single peptide did not find.
        /// </summary>
        [Test]
        public void TestSinglePeptideDidNotFind()
        {
            string uimfFileLocation = Cheetah;
            //string netAlignmentFileLocation = @"..\..\..\testFiles\Sarc_MS2_90_6Apr11_Cheetah_11-02-19_NetAlign.csv";

            //IInterpolation interpolation = AlignmentImporter.ReadFile(netAlignmentFileLocation);

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.2,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            string peptide = "TILDDLRAEDHFSVIDFNQNIR";
            double net = 0.48228999972343445;

            List<Modification> modificationList = new List<Modification>();
            //modificationList.Add(Modification.Oxidation);
            //modificationList.Add(Modification.Carbamidomethylation);
            //modificationList.Add(Modification.Carbamidomethylation);

            PeptideTarget target = new PeptideTarget(1, peptide, net, modificationList);

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters, null);
            var correlationResult = lcImsPeptideSearchWorkfow.RunInformedWorkflow(target);

            Console.WriteLine(correlationResult.ReferenceLcImsTargetResult.NormalizedElutionTime);
        }

        /// <summary>
        /// The test interesting peptide that has overlapping interference towards end of elution.
        /// </summary>
        [Test]
        public void TestInterestingPeptideThatHasOverlappingInterferenceTowardsEndOfElution()
        {
            string uimfFileLocation = @"..\..\..\testFiles\Sarc_MS2_90_6Apr11_Cheetah_11-02-19.uimf";
            string netAlignmentFileLocation = @"..\..\..\testFiles\Sarc_MS2_90_6Apr11_Cheetah_11-02-19_NetAlign.csv";

            IInterpolation interpolation = AlignmentImporter.ReadFile(netAlignmentFileLocation);

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.03,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            string peptide = "LCHCPVGYTGPFCDVDTK";
            double net = 0.3041;

            List<Modification> modificationList = new List<Modification>();
            //modificationList.Add(Modification.Oxidation);
            modificationList.Add(Modification.Carbamidomethylation);
            modificationList.Add(Modification.Carbamidomethylation);
            modificationList.Add(Modification.Carbamidomethylation);

            PeptideTarget target = new PeptideTarget(1, peptide, net, modificationList);

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters, interpolation);
            var correlationResult = lcImsPeptideSearchWorkfow.RunInformedWorkflow(target);

            Console.WriteLine(correlationResult.ReferenceLcImsTargetResult.NormalizedElutionTime);
        }

        /// <summary>
        /// The test data extraction speed single Target.
        /// </summary>
        [Test]
        public void TestDataExtractionSpeedSingleTarget()
        {
            string uimfFileLocation = Cheetah;

            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.1,
                IsotopicFitScoreThreshold = 0.15,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            string peptide = "QGHNSVFLIKGDK";
            double net = 0.2493;

            PeptideTarget target = new PeptideTarget(1, peptide, net);
            List<PeptideTarget> targetList = new List<PeptideTarget>();
            targetList.Add(target);

            LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileLocation, parameters);
            lcImsPeptideSearchWorkfow.ExtractData(targetList);
        }

        /// <summary>
        /// The test data extraction speed many targets.
        /// </summary>
        [Test]
        public void TestDataExtractionSpeedManyTargets()
        {
            // Setup Informed workflow parameters
            LcImsPeptideSearchParameters parameters = new LcImsPeptideSearchParameters
            {
                ChargeStateMax = 5,
                NetTolerance = 0.03,
                IsotopicFitScoreThreshold = 0.2,
                MassToleranceInPpm = 30,
                NumPointForSmoothing = 9
            };

            Console.WriteLine(DateTime.Now + ": Reading ViperCompatibleMass Tags from MTDB");
            List<PeptideTarget> targetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789");

            Console.WriteLine(DateTime.Now + ": Using " + targetList.Count + " targets.");

            // Put all UIMF Files to process here
            List<string> uimfFileList = new List<string>();
            uimfFileList.Add(Cheetah);

            // Iterate over each UIMF File
            for (int i = 0; i < uimfFileList.Count; i++)
            {
                string uimfFileLocation = uimfFileList[i];
                FileInfo uimfFileInfo = new FileInfo(uimfFileLocation);
                Console.WriteLine(DateTime.Now + ": Processing " + uimfFileInfo.Name);

                // NET Alignment
                string netAlignmentFileName = uimfFileInfo.Name.Replace(".uimf", "_NetAlign.csv");
                string netAlignmentLocation = Path.Combine(uimfFileInfo.DirectoryName, netAlignmentFileName);
                FileInfo netAlignmentFileInfo = new FileInfo(netAlignmentLocation);
                if (!File.Exists(netAlignmentFileInfo.FullName))
                {
                    
                }
                else
                {
                    Console.WriteLine(DateTime.Now + ": Using existing alignment file");
                }

                // Grab the net alignment
                IInterpolation interpolation = AlignmentImporter.ReadFile(netAlignmentFileInfo.FullName);

                //LcImsPeptideSearchWorkfow warmupInformedWorkflow = new LcImsPeptideSearchWorkfow(uimfFileInfo.FullName, parameters, interpolation);

                //foreach (var Target in targetList.Take(10))
                //{
                //    warmupInformedWorkflow.RunInformedWorkflow(Target);
                //}

                //warmupInformedWorkflow.PrintFeatureFindStatistics();

                //Console.WriteLine("**************************************************************");
                //Console.WriteLine("**************************************************************");
                //Console.WriteLine("**************************************************************");

                LcImsPeptideSearchWorkfow lcImsPeptideSearchWorkfow = new LcImsPeptideSearchWorkfow(uimfFileInfo.FullName, parameters, interpolation);

                foreach (var target in targetList)
                {
                    lcImsPeptideSearchWorkfow.RunInformedWorkflow(target);
                }

                //LcImsPeptideSearchWorkfow.PrintFeatureFindStatistics();

                // Warmup
                //LcImsPeptideSearchWorkfow.ExtractData(targetList.Take(10)); 

                // Actual
                //Random random = new Random();
                //LcImsPeptideSearchWorkfow.ExtractData(targetList.OrderBy(X => random.Next()).Take(100));
                //LcImsPeptideSearchWorkfow.ExtractData(targetList);
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

        /// <summary>
        /// The test UIMF file reading.
        /// </summary>
        [Test]
        public void TestUimFfileReading() 
        {
            double[] mzq = new double[200];
            int[] intensity = new int[100];

            DataReader uimfReader = new DataReader(Cheetah);
            Console.WriteLine("\r\nReading UIMF file at: " + uimfReader.UimfFilePath);
            Console.WriteLine(uimfReader.GetGlobalParams().NumFrames);
            //Console.WriteLine(uimfReader.GetXic(161.1078678, 30, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM));
            //Console.WriteLine(uimfReader.GetXic(20, DataReader.FrameType.MS1));
            //Console.WriteLine(uimfReader.GetXic(161.107, 30, 4, 100, 0, 100, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM));
            //Console.WriteLine("\r\nSpectrum: " + uimfReader.GetSpectrum(1, DataReader.FrameType.MS1, 2, out mzq, out intensity));
            //foreach (var item in intensity)
            //    Console.WriteLine(item);
        }
    }
}
