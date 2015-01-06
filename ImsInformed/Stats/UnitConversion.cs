// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnitConversion.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the UnitConversion type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Stats
{
    /// <summary>
    /// The unit conversion.
    /// </summary>
    public class UnitConversion
    {
        /// <summary>
        /// The absolute zero.
        /// </summary>
        public const double AbsoluteZero = 273.15;

        /// <summary>
        /// The atmospheric pressure.
        /// </summary>
        public const double AtmosphericPressure = 101325;

        /// <summary>
        /// The torr 2 pascal.
        /// </summary>
        /// <param name="pressureInTorr">
        /// The pressure in torr.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double Torr2Pascal(double pressureInTorr)
        {
            return 133.322368 * pressureInTorr;
        }

        /// <summary>
        /// The torr 2 nondimensionalized.
        /// </summary>
        /// <param name="pressureInTorr">
        /// The pressure in torr.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double Torr2Nondimensionalized(double pressureInTorr)
        {
            return (133.322368 * pressureInTorr) / AtmosphericPressure;
        }

        /// <summary>
        /// The degree celsius 2 kelvin.
        /// </summary>
        /// <param name="temperature">
        /// The temperature.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double DegreeCelsius2Kelvin(double temperature)
        {
            return AbsoluteZero + temperature;
        }

        /// <summary>
        /// The degree celsius 2 nondimensionalized.
        /// </summary>
        /// <param name="temperature">
        /// The temperature.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double DegreeCelsius2Nondimensionalized(double temperature)
        {
            return DegreeCelsius2Kelvin(temperature) / AbsoluteZero;
        }
    }
}
