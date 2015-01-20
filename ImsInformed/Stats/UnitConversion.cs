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
        public const double AbsoluteZeroInKelvin = 273.15;

        /// <summary>
        /// The room temperature.
        /// </summary>
        public const double RoomTemperatureInKelvin = 298.15;

        /// <summary>
        /// The standard ims pressure in TORR.
        /// </summary>
        public const double StandardImsPressureInTorr = 4;

        /// <summary>
        /// The pascal unit TORR.
        /// </summary>
        public const double PascalPerTorr = 133.322368;

        /// <summary>
        /// The atmospheric pressure.
        /// </summary>
        public const double AtmosphericPressureInPascal = 101325;

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
            return PascalPerTorr * pressureInTorr;
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
            return (PascalPerTorr * pressureInTorr) / AtmosphericPressureInPascal;
        }

        public static double Nondimensionalized2Torr(double pressureNondimensionalized)
        {
            return pressureNondimensionalized * AtmosphericPressureInPascal / PascalPerTorr;
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
            return AbsoluteZeroInKelvin + temperature;
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
            return DegreeCelsius2Kelvin(temperature) / AbsoluteZeroInKelvin;
        }

        public static double Nondimensionalized2Kelvin(double temperature)
        {
            return temperature * AbsoluteZeroInKelvin;
        }
    }
}
