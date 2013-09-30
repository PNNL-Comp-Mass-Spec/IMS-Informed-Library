using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImsInformed.Domain;
using ImsInformed.IO;
using ImsInformed.Parameters;
using ImsInformed.Util;
using NUnit.Framework;

namespace ImsInformedTests
{
	public class FunctionalTests
	{
		[Test]
		public void TestSinglePeptide()
		{
			string uimfFileLocation = @"..\..\..\testFiles\Sarc_P23_C07_2143_23Feb12_Cheetah_11-05-40.uimf";

			InformedParameters parameters = new InformedParameters
			{
			    ChargeStateMax = 5,
			    DriftTimeTolerance = 100,
			    NetTolerance = 0.1,
			    IsotopicFitScoreMax = 0.15,
			    MassToleranceInPpm = 30,
			    NumPointForSmoothing = 9
			};

			string peptide = "DGWHSWPIAHQWPQGPSAVDAAFSWEEK";
			double net = 0.4832;

			ImsTarget target = new ImsTarget(peptide, net);

			InformedWorkflow informedWorkflow = new InformedWorkflow(uimfFileLocation, parameters);
			informedWorkflow.RunInformedWorkflow(target);
		}

		[Test]
		public void TestSaturatedPeptide()
		{
			string uimfFileLocation = @"..\..\..\testFiles\Sarc_P23_C07_2143_23Feb12_Cheetah_11-05-40.uimf";

			InformedParameters parameters = new InformedParameters
			{
				ChargeStateMax = 5,
				DriftTimeTolerance = 100,
				NetTolerance = 0.1,
				IsotopicFitScoreMax = 0.15,
				MassToleranceInPpm = 30,
				NumPointForSmoothing = 9
			};

			string peptide = "QGHNSVFLIKGDK";
			double net = 0.2493;

			ImsTarget target = new ImsTarget(peptide, net);

			InformedWorkflow informedWorkflow = new InformedWorkflow(uimfFileLocation, parameters);
			informedWorkflow.RunInformedWorkflow(target);
		}

		[Test]
		public void TestImportTargets()
		{
			List<ImsTarget> targetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789");
			Console.WriteLine(targetList.Count);
		}

		[Test]
		public void TestRunAllTargets()
		{
			string uimfFileLocation = @"..\..\..\testFiles\Sarc_P23_C07_2143_23Feb12_Cheetah_11-05-40.uimf";

			InformedParameters parameters = new InformedParameters
			{
				ChargeStateMax = 5,
				DriftTimeTolerance = 100,
				NetTolerance = 0.1,
				IsotopicFitScoreMax = 0.15,
				MassToleranceInPpm = 30,
				NumPointForSmoothing = 9
			};

			List<ImsTarget> targetList = MassTagImporter.ImportMassTags("elmer", "MT_Human_Sarcopenia_P789");

			InformedWorkflow informedWorkflow = new InformedWorkflow(uimfFileLocation, parameters);

			foreach (var imsTarget in targetList)
			{
				informedWorkflow.RunInformedWorkflow(imsTarget);	
			}
		}
	}
}
