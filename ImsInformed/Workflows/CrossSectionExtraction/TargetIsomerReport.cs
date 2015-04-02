namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using System;

    /// <summary>
    /// The isomer result.
    /// </summary>
    [Serializable]
    public struct TargetIsomerReport
    {
        /// <summary>
        /// The cross sectional area.
        /// </summary>
        public double CrossSectionalArea;

        /// <summary>
        /// The mobility.
        /// </summary>
        public double Mobility;

        #region data needed by viper
        /// <summary>
        /// The cross sectional area.
        /// </summary>
        public double LastVoltageGroupDriftTimeInMs;

        /// <summary>
        /// The monoisotopic mass.
        /// </summary>
        public double MonoisotopicMass;
        #endregion
    }
}
