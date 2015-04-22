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

    using PNNLOmics.Data;

    /// <summary>
    /// The peptide Target.
    /// </summary>
    [Serializable]
    public class PeptideTarget : IImsTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PeptideTarget"/> class. 
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
            this.CompositionWithoutAdduct = PeptideUtil.GetCompositionOfPeptide(peptideSequence);

            if (modificationList != null)
            {
                foreach (var modification in modificationList)
                {
                    this.CompositionWithoutAdduct += modification.Composition;
                }
            }

            this.ID = id;
            this.PeptideSequence = peptideSequence;
            this.MonoisotopicMass = this.CompositionWithoutAdduct.Mass;
            this.NormalizedElutionTime = normalizedElutionTime;
            this.CompositionWithoutAdduct = this.CompositionWithoutAdduct;
            this.EmpiricalFormula = this.CompositionWithoutAdduct.ToPlainString();
            this.ResultList = new List<LcImsTargetResult>();
            this.ModificationList = modificationList;
            this.TargetType = TargetType.Peptide;
            this.Adduct = new IonizationAdduct(IonizationMethod.ProtonPlus);
            this.DriftTimeTargetList = new List<DriftTimeTarget>();
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
        /// Gets the adduct.
        /// </summary>
        public IonizationAdduct Adduct { get; private set; }

        /// <summary>
        /// Gets the Target type.
        /// </summary>
        public TargetType TargetType { get; private set; }

        /// <summary>
        /// Gets the mass.
        /// </summary>
        public double MonoisotopicMass { get; private set; }

        /// <summary>
        /// Gets the empirical formula.
        /// </summary>
        public string EmpiricalFormula { get; private set; }

        /// <summary>
        /// Gets the composition without adduct.
        /// </summary>
        public Composition CompositionWithoutAdduct { get; private set; }

        /// <summary>
        /// Gets the composition with adduct.
        /// </summary>
        public Composition CompositionWithAdduct 
        {
            get
            {
                throw new Exception("Adduct is not specified in Peptide targets.");
            }
        }

        /// <summary>
        /// Gets the normalized elution time.
        /// </summary>
        public double NormalizedElutionTime { get; private set; }

        /// <summary>
        /// Gets the drift time Target list.
        /// </summary>
        public IList<DriftTimeTarget> DriftTimeTargetList { get; set; } 

        /// <summary>
        /// Gets the Target descriptor.
        /// </summary>
        public string TargetDescriptor
        {
            get
            {
                return this.PeptideSequence + this.Adduct;
            }
        }

        /// <summary>
        /// Gets the chemical identifier.
        /// </summary>
        public string ChemicalIdentifier
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
        /// Gets or sets the Target MZ.
        /// </summary>
        public double MassWithAdduct { get; set; }

        public int ChargeState { get; private set; }

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
            massTagQuery.Append(this.MonoisotopicMass);
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
            //massTagQuery.Append(this.MonoisotopicMass);
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
            //    conformerQuery.Append(driftTimeTarget.NormalizedDriftTimeInMs);
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

                IEnumerable<DriftTimeTarget> possibleDriftTimeTargets = this.DriftTimeTargetList.Where(x => x.ChargeState == chargeState).OrderBy(x => Math.Abs(x.NormalizedDriftTimeInMs - driftTime));

                double targetDriftTime = 0;
                double driftTimeError = 0;

                if (possibleDriftTimeTargets.Any())
                {
                    DriftTimeTarget driftTimeTarget = possibleDriftTimeTargets.First();
                    targetDriftTime = driftTimeTarget.NormalizedDriftTimeInMs;
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

        public bool Equals(IImsTarget other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.TargetType != TargetType.Peptide) return false;
            return this.Equals(other as PeptideTarget);
        }

        public bool Equals(PeptideTarget other)
        {
            return MoleculeUtil.AreCompositionsEqual(this.CompositionWithAdduct, other.CompositionWithAdduct) && 
                this.NormalizedElutionTime == other.NormalizedElutionTime &&
                this.TargetDescriptor == other.TargetDescriptor && 
                this.PeptideSequence == other.PeptideSequence;
        }

        public override bool Equals(object other) 
        {
            return this.Equals(other as IImsTarget);
        }

        /// <summary>
        /// The get hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode() 
        {
            int result = 29;
            result = result * 13 + this.CompositionWithAdduct.ToPlainString().GetHashCode();
            result = result * 13 + this.ChargeState;
            result = result * 13 + this.TargetDescriptor.GetHashCode();
            result = result * 13 + this.PeptideSequence.GetHashCode();
            return result;
        }
    }
}
