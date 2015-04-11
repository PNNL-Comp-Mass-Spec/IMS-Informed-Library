namespace ImsInformed.IO
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Runtime.CompilerServices;

    using ImsInformed.Domain;
    using ImsInformed.Targets;

    using InformedProteomics.Backend.Data.Sequence;

    public class MassTagImporter
    {
        private const string DB_USERNAME = "mtuser";
        private const string DB_PASSWORD = "mt4fun";

        private static Dictionary<string, Modification> _dmsModToInformedModMap;

        static MassTagImporter()
        {
            _dmsModToInformedModMap = new Dictionary<string, Modification> {{"IodoAcet", Modification.Carbamidomethylation}, {"Plus1Oxy", Modification.Oxidation}};
        }

        public static List<PeptideTarget> ImportMassTags(string serverName, string databaseName, double maxMsgfSpecProb = 1e-10, bool isForCalibration = false)
        {
            List<PeptideTarget> targetList = new List<PeptideTarget>();

            // Build connection string
            string connectionString = BuildConnectionString(serverName, databaseName);

            // Using connection
            var dbFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            using (var connection = dbFactory.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();

                // Execute query
                string queryString = isForCalibration ? GetQueryForCalibration() : GetQuery();
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = queryString;
                    command.CommandTimeout = 120;
                    DbDataReader reader = command.ExecuteReader();

                    PeptideTarget currentImsTarget = null;

                    while (reader.Read())
                    {
                        int massTagId = Convert.ToInt32(reader["Mass_Tag_ID"]);
                        string peptide = Convert.ToString(reader["peptide"]);
                        double normalizedElutionTime = Convert.ToDouble(reader["Avg_GANET"]);
                        int modCount = Convert.ToInt16(reader["Mod_Count"]);
                        
                        List<Modification> modificationList = new List<Modification>();
                        
                        if(modCount > 0)
                        {
                            string modificationString = Convert.ToString(reader["Mod_Description"]);
                            string[] splitModString = modificationString.Split(',');
                            foreach (var singleModString in splitModString)
                            {
                                string modificationName = singleModString.Split(':')[0];
                                Modification modification = _dmsModToInformedModMap[modificationName];
                                modificationList.Add(modification);
                            }
                        }

                        bool isSameTarget = IsSameTarget(currentImsTarget, peptide, normalizedElutionTime, modificationList);

                        if(!isSameTarget)
                        {
                            currentImsTarget = new PeptideTarget(massTagId, peptide, normalizedElutionTime, modificationList);
                            targetList.Add(currentImsTarget);
                        }

                        int chargeState = Convert.ToInt16(reader["Conformer_Charge"]);
                        double driftTime = Convert.ToDouble(reader["Drift_Time_Avg"]);

                        DriftTimeTarget driftTimeTarget = new DriftTimeTarget(massTagId.ToString(CultureInfo.InvariantCulture), driftTime, currentImsTarget.EmpiricalFormula, new IonizationAdduct(chargeState));
                        currentImsTarget.DriftTimeTargetList.Add(driftTimeTarget);
                    }
                }
            }
            
            return targetList;
        }

        public static bool IsSameTarget(PeptideTarget currentImsTarget, string peptide, double normalizedElutionTime, List<Modification> modificationList)
        {
            int modCount = modificationList.Count;

            if (currentImsTarget != null)
            {
                if (currentImsTarget.PeptideSequence == peptide && Math.Abs(currentImsTarget.NormalizedElutionTime - normalizedElutionTime) < 0.00001)
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
                        "MTN.Avg_GANET," +
                        "MT.Mod_Count," +
                        "MT.Mod_Description," +
                        "MTC.Charge AS Conformer_Charge, " +
                        "MTC.Conformer, " +
                        "MTC.Drift_Time_Avg " +
                    "FROM T_Mass_Tags MT " +
                        "JOIN T_Mass_Tags_NET AS MTN ON MT.Mass_Tag_ID = MTN.Mass_Tag_ID " +
                        "JOIN T_Mass_Tag_Conformers_Observed MTC ON MT.Mass_Tag_ID = MTC.Mass_Tag_ID " +
                    "WHERE MT.PMT_Quality_Score >= 2 " +
                    "ORDER BY MT.Monoisotopic_Mass, MT.Mass_Tag_ID";
        }

        public static string GetQueryForCalibration()
        {
            return "SELECT " +
                        "MT.Mass_Tag_ID, " +
                        "MT.Peptide, " +
                        "MTN.Avg_GANET," +
                        "MT.Mod_Count," +
                        "MT.Mod_Description," +
                        "MTC.Charge AS Conformer_Charge, " +
                        "MTC.Conformer, " +
                        "MTC.Drift_Time_Avg " +
                    "FROM T_Mass_Tags MT " +
                        "JOIN T_Mass_Tags_NET AS MTN ON MT.Mass_Tag_ID = MTN.Mass_Tag_ID " +
                        "JOIN T_Mass_Tag_Conformers_Observed MTC ON MT.Mass_Tag_ID = MTC.Mass_Tag_ID " +
                    "WHERE MT.PMT_Quality_Score >= 3 AND MT.Number_Of_Peptides > 50 " +
                    "ORDER BY MT.Monoisotopic_Mass, MT.Mass_Tag_ID";
        }
    }
}
