// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImsTarget.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The ims target.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Sequence;

    /// <summary>
    /// The IMS target.
    /// </summary>
    public class ImsTarget
    {
        /// <summary>
        /// The peptide sequence.
        /// </summary>
        private string peptideSequence;

        /// <summary>
        /// The drift time target list.
        /// </summary>
        private IList<DriftTimeTarget> driftTimeTargetList;

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
        public ImsTarget(int id, string peptideSequence, double normalizedElutionTime, IList<Modification> modificationList = null)
        {
            Composition composition = PeptideUtil.GetCompositionOfPeptide(peptideSequence);
            foreach (var modification in modificationList)
            {
                composition += modification.Composition;
            }

            for (int i = 0; i < 11; i++)
            {
                composition += Composition.Hydrogen;
            }

            this.Id = id;
            this.PeptideSequence = peptideSequence;
            this.Mass = composition.Mass;
            this.NormalizedElutionTime = normalizedElutionTime;
            this.Composition = composition;
            this.EmpiricalFormula = this.Composition.ToPlainString();
            this.DriftTimeTargetList = new List<DriftTimeTarget>();
            this.ResultList = new List<ImsTargetResult>();
            this.ModificationList = modificationList;
            this.TargetMz = 0;
            this.TargetType = TargetType.Peptide;
            this.IonizationType = IonizationMethod.ProtonPlus;
        }

        /// <summary>
        /// Constructor for non peptides with composition
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        public ImsTarget(int id, IonizationMethod ionization, string empiricalFormula)
        {
            this.Id = id;
            this.EmpiricalFormula = empiricalFormula;

            // parse the small molecule empirical formula
            this.Composition = MoleculeUtil.ReadEmpiricalFormula(empiricalFormula);
            this.Mass = this.Composition.Mass;
            this.ResultList = new List<ImsTargetResult>();
            this.DriftTimeTargetList = new List<DriftTimeTarget>();
            this.ModificationList = null;
            this.TargetType = TargetType.SmallMolecule;
            this.IonizationType = ionization;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImsTarget"/> class.
        /// </summary>
        /// <param name="id">
        /// The ID.
        /// </param>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="targetMz">
        /// The target MZ.
        /// </param>
        public ImsTarget(int id, IonizationMethod ionization, double targetMz)
        {
            this.Id = id;
            this.TargetMz = targetMz;
            this.TargetType = TargetType.SmallMolecule;
            this.IonizationType = ionization;

            this.ResultList = new List<ImsTargetResult>();
            this.DriftTimeTargetList = new List<DriftTimeTarget>();
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets the ionization type.
        /// </summary>
        public IonizationMethod IonizationType { get; private set; }

        /// <summary>
        /// Gets the target type.
        /// </summary>
        public TargetType TargetType { get; private set; }

        /// <summary>
        /// Gets the empirical formula.
        /// </summary>
        public string EmpiricalFormula { get; private set; }

        /// <summary>
        /// Gets the composition.
        /// </summary>
        public Composition Composition { get; private set; }

        /// <summary>
        /// Gets the PeptideSequence.
        /// </summary>
        public string PeptideSequence
        {
            get
            {
                if (this.TargetType != TargetType.Peptide)
                {
                    throw new InvalidOperationException("Cannot get peptide sequence for targets that's not a peptide");
                }

                return this.peptideSequence;
            }

            private set
            {
                if (this.TargetType != TargetType.Peptide)
                {
                    throw new InvalidOperationException("Cannot get peptide sequence for targets that's not a peptide");
                }

                this.peptideSequence = value;
            }
        }

        /// <summary>
        /// Gets the mass.
        /// </summary>
        public double Mass { get; private set; }

        /// <summary>
        /// Gets the normalized elution time.
        /// </summary>
        public double NormalizedElutionTime { get; private set; }

        /// <summary>
        /// Gets or sets the drift time target list.
        /// </summary>
        public IList<DriftTimeTarget> DriftTimeTargetList
        {
            get
            {
                if (this.TargetType != TargetType.Peptide)
                {
                    throw new InvalidOperationException("Cannot get peptide sequence for targets that's not a peptide");
                }

                return this.driftTimeTargetList;
            }

            set
            {
                if (this.TargetType != TargetType.Peptide)
                {
                    throw new InvalidOperationException("Cannot get peptide sequence for targets that's not a peptide");
                }

                this.driftTimeTargetList = value;
            }
        }

        /// <summary>
        /// Gets or sets the result list.
        /// </summary>
        public IList<ImsTargetResult> ResultList { get; set; }

        /// <summary>
        /// Gets the modification list.
        /// </summary>
        public IList<Modification> ModificationList { get; private set; }

        /// <summary>
        /// Gets or sets the target MZ.
        /// </summary>
        public double TargetMz { get; set; }

        /// <summary>
        /// The remove results.
        /// </summary>
        public void RemoveResults()
        {
            this.ResultList = new List<ImsTargetResult>();
        }

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
            massTagQuery.Append(this.Id);
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
                resultQuery.Append(this.Id);
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
