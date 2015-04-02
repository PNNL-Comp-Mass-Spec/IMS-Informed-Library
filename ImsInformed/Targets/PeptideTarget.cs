// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PeptideTarget.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the PeptideTarget type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Targets
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using ImsInformed.Domain;
    using ImsInformed.Interfaces;
    using ImsInformed.Util;
    using ImsInformed.Workflows.LcImsPeptideExtraction;

    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Sequence;

    /// <summary>
    /// The peptide target.
    /// </summary>
    public class PeptideTarget : IImsTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImsTarget"/> class.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="peptideSequence">
        /// The PeptideSequence.
        /// </param>
        /// <param name="normalizedElutionTime">
        /// The normalized elution time.
        /// </param>
        /// <param name="modificationList">
        /// The modification list.
        /// </param>
        public PeptideTarget(int id, string peptideSequence, double normalizedElutionTime, IList<Modification> modificationList = null)
        {
            this.Composition = PeptideUtil.GetCompositionOfPeptide(peptideSequence);

            if (modificationList != null)
            {
                foreach (var modification in modificationList)
                {
                    this.Composition += modification.Composition;
                }
            }

            this.ID = id;
            this.PeptideSequence = peptideSequence;
            this.Mass = this.Composition.Mass;
            this.NormalizedElutionTime = normalizedElutionTime;
            this.Composition = this.Composition;
            this.EmpiricalFormula = this.Composition.ToPlainString();
            this.ResultList = new List<LcImsTargetResult>();
            this.ModificationList = modificationList;
            this.TargetType = TargetType.Peptide;
            this.IonizationType = IonizationMethod.ProtonPlus;
        }

        /// <summary>
        /// The peptide sequence.
        /// </summary>
        public string PeptideSequence { get; private set; }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Gets the ionization type.
        /// </summary>
        public IonizationMethod IonizationType { get; private set; }

        /// <summary>
        /// Gets the target type.
        /// </summary>
        public TargetType TargetType { get; private set; }

        /// <summary>
        /// Gets the mass.
        /// </summary>
        public double Mass { get; private set; }

        /// <summary>
        /// Gets the empirical formula.
        /// </summary>
        public string EmpiricalFormula { get; private set; }

        public Composition Composition { get; private set; }

        /// <summary>
        /// Gets the normalized elution time.
        /// </summary>
        public double NormalizedElutionTime { get; private set; }

        /// <summary>
        /// Gets the drift time target list.
        /// </summary>
        public IList<DriftTimeTarget> DriftTimeTargetList { get; private set; } 

        /// <summary>
        /// Gets the target descriptor.
        /// </summary>
        public string TargetDescriptor
        {
            get
            {
                return this.PeptideSequence;
            }
        }

        /// <summary>
        /// Gets the modification list.
        /// </summary>
        public IList<Modification> ModificationList { get; private set; }

        /// <summary>
        /// Gets the result list.
        /// </summary>
        public IList<LcImsTargetResult> ResultList { get; private set; } 

        /// <summary>
        /// Gets or sets the target MZ.
        /// </summary>
        public double TargetMz { get; set; }

        /// <summary>
        /// The create SQL mass tag queries.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string CreateSqlMassTagQueries()
        {
            StringBuilder massTagQuery = new StringBuilder();
            massTagQuery.Append("INSERT INTO T_MASS_TAG (Mass_Tag_Id, PeptideSequence, Mod_Description, Empirical_Formula, Monoisotopic_Mass, NET) VALUES(");
            massTagQuery.Append(this.ID);
            massTagQuery.Append(",");
            massTagQuery.Append("'" + this.PeptideSequence + "'");
            massTagQuery.Append(",");
            massTagQuery.Append("'MOD_HERE'");
            massTagQuery.Append(",");
            massTagQuery.Append("'" + this.EmpiricalFormula + "'");
            massTagQuery.Append(",");
            massTagQuery.Append(this.Mass);
            massTagQuery.Append(",");
            massTagQuery.Append(this.NormalizedElutionTime);
            massTagQuery.Append(");");

            return massTagQuery.ToString();
        }

        /// <summary>
        /// The remove results.
        /// </summary>
        public void RemoveResults()
        {
            this.ResultList = new List<LcImsTargetResult>();
        }

        /// <summary>
        /// The create SQL result queries.
        /// </summary>
        /// <param name="datasetId">
        /// The dataset id.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string CreateSqlResultQueries(int datasetId)
        {
            StringBuilder allQueries = new StringBuilder();

            //StringBuilder massTagQuery = new StringBuilder();
            //massTagQuery.Append("INSERT INTO T_MASS_TAG (Mass_Tag_Id, PeptideSequence, Mod_Description, Empirical_Formula, Monoisotopic_Mass, NET) VALUES(");
            //massTagQuery.Append(this.Id);
            //massTagQuery.Append(",");
            //massTagQuery.Append("'" + this.PeptideSequence + "'");
            //massTagQuery.Append(",");
            //massTagQuery.Append("'MOD_HERE'");
            //massTagQuery.Append(",");
            //massTagQuery.Append("'" + this.EmpiricalFormula + "'");
            //massTagQuery.Append(",");
            //massTagQuery.Append(this.Mass);
            //massTagQuery.Append(",");
            //massTagQuery.Append(this.NormalizedElutionTime);
            //massTagQuery.Append(");");

            //allQueries.Append(massTagQuery.ToString());

            //foreach (var driftTimeTarget in this.DriftTimeTargetList)
            //{
            //    StringBuilder conformerQuery = new StringBuilder();
            //    conformerQuery.Append("INSERT INTO T_MASS_TAG_Conformer (Mass_Tag_Id, Charge_State, Drift_Time) VALUES(");
            //    conformerQuery.Append(this.Id);
            //    conformerQuery.Append(",");
            //    conformerQuery.Append(driftTimeTarget.ChargeState);
            //    conformerQuery.Append(",");
            //    conformerQuery.Append(driftTimeTarget.DriftTime);
            //    conformerQuery.Append(");");
            //    allQueries.Append("\n");
            //    allQueries.Append(conformerQuery.ToString());
            //}

            foreach (var imsTargetResult in this.ResultList)
            {
                double observedMz = imsTargetResult.IsotopicProfile != null ? imsTargetResult.IsotopicProfile.MonoPeakMZ : 0;
                double abundance = imsTargetResult.IsotopicProfile != null ? imsTargetResult.IsotopicProfile.GetAbundance() : 0;
                int chargeState = imsTargetResult.ChargeState;
                double driftTime = imsTargetResult.DriftTime;

                IEnumerable<DriftTimeTarget> possibleDriftTimeTargets = this.DriftTimeTargetList.Where(x => x.ChargeState == chargeState).OrderBy(x => Math.Abs(x.DriftTime - driftTime));

                double targetDriftTime = 0;
                double driftTimeError = 0;

                if (possibleDriftTimeTargets.Any())
                {
                    DriftTimeTarget driftTimeTarget = possibleDriftTimeTargets.First();
                    targetDriftTime = driftTimeTarget.DriftTime;
                    driftTimeError = driftTime - targetDriftTime;
                }

                double elutionTimeError = imsTargetResult.NormalizedElutionTime - this.NormalizedElutionTime;

                StringBuilder resultQuery = new StringBuilder();
                resultQuery.Append("INSERT INTO T_Result (Mass_Tag_Id, Dataset_Id, Charge_State, Observed_Mz, Ppm_Error, Scan_Lc, Net, Net_Error, Drift_Time, Drift_Time_Error, Isotopic_Fit_Score, Abundance, Charge_Correlation, Failure_Reason) VALUES(");
                resultQuery.Append(this.ID);
                resultQuery.Append(",");
                resultQuery.Append(datasetId);
                resultQuery.Append(",");
                resultQuery.Append(chargeState);
                resultQuery.Append(",");
                resultQuery.Append(observedMz);
                resultQuery.Append(",");
                resultQuery.Append(imsTargetResult.PpmError);
                resultQuery.Append(",");
                resultQuery.Append(imsTargetResult.ScanLcRep);
                resultQuery.Append(",");
                resultQuery.Append(imsTargetResult.NormalizedElutionTime);
                resultQuery.Append(",");
                resultQuery.Append(elutionTimeError);
                resultQuery.Append(",");
                resultQuery.Append(driftTime);
                resultQuery.Append(",");
                resultQuery.Append(driftTimeError);
                resultQuery.Append(",");
                resultQuery.Append(imsTargetResult.IsotopicFitScore);
                resultQuery.Append(",");
                resultQuery.Append(abundance);
                resultQuery.Append(",");
                resultQuery.Append("0");
                resultQuery.Append(",");
                resultQuery.Append("'" + imsTargetResult.AnalysisStatus + "'");
                resultQuery.Append(");");

                allQueries.Append("\n");
                allQueries.Append(resultQuery.ToString());
            }

            return allQueries.ToString();
        }
    }
}
