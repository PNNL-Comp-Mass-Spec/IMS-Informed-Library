// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArrivalTimeSnapShot.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Snapshot of arrival time.
// </summary>
// --------------------------------------------------------------------------------------------------------------------



namespace ImsInformed.Workflows.CrossSectionExtraction
{
    /// <summary>
    /// Snapshot of arrival time.
    /// </summary>
    public struct ArrivalTimeSnapShot
    {
        /// <summary>
        /// The measured arrival time in ms.
        /// </summary>
        public double MeasuredArrivalTimeInMs;

        /// <summary>
        /// The drift tube voltage in volt.
        /// </summary>
        public double DriftTubeVoltageInVolt;

        /// <summary>
        /// The temperature in kelvin.
        /// </summary>
        public double TemperatureInKelvin;

        /// <summary>
        /// The pressure in torr.
        /// </summary>
        public double PressureInTorr;
    }
}