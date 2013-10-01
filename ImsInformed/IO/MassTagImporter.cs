using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using ImsInformed.Domain;
using InformedProteomics.Backend.Data.Sequence;

namespace ImsInformed.IO
{
	public class MassTagImporter
	{
		private const string DB_USERNAME = "mtuser";
		private const string DB_PASSWORD = "mt4fun";

		private static Dictionary<string, Modification> _dmsModToInformedModMap;

		static MassTagImporter()
		{
			_dmsModToInformedModMap = new Dictionary<string, Modification> {{"IodoAcet", Modification.Carbamidomethylation}, {"Plus1Oxy", Modification.Oxidation}};
		}

		public static List<ImsTarget> ImportMassTags(string serverName, string databaseName, double maxMsgfSpecProb = 1e-10)
		{
			List<ImsTarget> targetList = new List<ImsTarget>();

			// Build connection string
			string connectionString = BuildConnectionString(serverName, databaseName);

			// Using connection
			var dbFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
			using (var connection = dbFactory.CreateConnection())
			{
				connection.ConnectionString = connectionString;
				connection.Open();

				// Execute query
				string queryString = GetQuery();
				using (DbCommand command = connection.CreateCommand())
				{
					command.CommandText = queryString;
					command.CommandTimeout = 120;
					DbDataReader reader = command.ExecuteReader();

					ImsTarget currentImsTarget = null;

					while (reader.Read())
					{
						int massTagId = Convert.ToInt32(reader["Mass_Tag_ID"]);
						string peptide = Convert.ToString(reader["Peptide"]);
						double normalizedElutionTime = Convert.ToDouble(reader["Avg_GANET"]);
						int modCount = Convert.ToInt16(reader["Mod_Count"]);
						
						List<Modification> modificationList = new List<Modification>();
						
						if(modCount > 0)
						{
							string modificationName = Convert.ToString(reader["Mod_Name"]);
							modificationList.Add(_dmsModToInformedModMap[modificationName]);
						}

						bool isSameTarget = IsSameTarget(currentImsTarget, peptide, normalizedElutionTime, modificationList);

						if(!isSameTarget)
						{
							currentImsTarget = new ImsTarget(massTagId, peptide, normalizedElutionTime, modificationList);
							targetList.Add(currentImsTarget);
						}

						int chargeState = Convert.ToInt16(reader["Conformer_Charge"]);
						double driftTime = Convert.ToDouble(reader["Drift_Time_Avg"]);

						DriftTimeTarget driftTimeTarget = new DriftTimeTarget(chargeState, driftTime);
						currentImsTarget.DriftTimeTargetList.Add(driftTimeTarget);
					}
				}
			}
			
			return targetList;
		}

		public static bool IsSameTarget(ImsTarget currentImsTarget, string peptide, double normalizedElutionTime, List<Modification> modificationList)
		{
			int modCount = modificationList.Count;

			if (currentImsTarget != null)
			{
				if (currentImsTarget.Peptide == peptide && Math.Abs(currentImsTarget.NormalizedElutionTime - normalizedElutionTime) < 0.00001)
				{
					if (modCount == currentImsTarget.ModificationList.Count)
					{
						if (modCount == 0) return true;
						else
						{
							if (currentImsTarget.ModificationList[0] == modificationList[0]) return true;
						}
					}
				}
			}

			return false;
		}

		public static string BuildConnectionString(string serverName, string databaseName)
		{
			SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
			{
			    UserID = DB_USERNAME,
			    Password = DB_PASSWORD,
			    DataSource = serverName,
			    InitialCatalog = databaseName,
			    ConnectTimeout = 5
			};

			return builder.ConnectionString;
		}

		public static string GetQuery()
		{
			return "SELECT " +
						"MT.Mass_Tag_ID, " +
						"MT.Peptide, " +
						"MT.Monoisotopic_Mass, " +
						"MTN.Avg_GANET," +
						"MTN.Cnt_GANET AS NET_Obs_Count," +
						"MTN.PNET," +
						"MT.Mod_Count," +
						"MT.Mod_Description," +
						"MT.Min_MSGF_SpecProb," +
						"MTC.Conformer_ID, " +
						"MTC.Charge AS Conformer_Charge, " +
						"MTC.Conformer, " +
						"MTC.Drift_Time_Avg, " +
						"MTC.Drift_Time_StDev,"  +
						"MTC.Obs_Count AS Conformer_Obs_Count, " + 
						"MTMI.Mod_Name, " +
						"MTMI.Mod_Position, " +
						"MCF.Empirical_Formula " +
					"FROM T_Mass_Tags MT " +
						"JOIN T_Mass_Tags_NET AS MTN ON MT.Mass_Tag_ID = MTN.Mass_Tag_ID " +
						"JOIN T_Mass_Tag_Conformers_Observed MTC ON MT.Mass_Tag_ID = MTC.Mass_Tag_ID " +
						"LEFT JOIN T_Mass_Tag_Mod_Info MTMI ON MT.Mass_Tag_ID = MTMI.Mass_Tag_ID " +
						"LEFT JOIN MT_Main.dbo.V_DMS_Mass_Correction_Factors MCF ON MTMI.Mod_Name = MCF.Mass_Correction_Tag " +
					"WHERE MT.PMT_Quality_Score >= 2 " +
					"ORDER BY MT.Monoisotopic_Mass, MT.Mass_Tag_ID, MTMI.Mod_Name";
		}
	}
}
