using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Domain
{
    /// <summary>
    /// The isomer result.
    /// </summary>
    [Serializable]
    public struct IsomerResult
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
