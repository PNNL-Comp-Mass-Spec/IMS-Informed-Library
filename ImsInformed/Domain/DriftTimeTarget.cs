// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DriftTimeTarget.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the DriftTimeTarget type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    /// <summary>
    /// The drift time target.
    /// </summary>
    public class DriftTimeTarget
    {
        /// <summary>
        /// Gets or sets the charge state.
        /// </summary>
        public int ChargeState { get; set; }

        public double DriftTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriftTimeTarget"/> class.
        /// </summary>
        /// <param name="chargeState">
        /// The charge state.
        /// </param>
        /// <param name="driftTime">
        /// The drift time.
        /// </param>
        public DriftTimeTarget(int chargeState, double driftTime)
        {
            this.ChargeState = chargeState;
            this.DriftTime = driftTime;
        }
    }
}
