// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnitConversion.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the UnitConversion type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Util
{
    using System;

    /// <summary>
    /// The unit conversion.
    /// </summary>
    internal class UnitConversion
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

        /// <summary>
        /// The nondimensionalized 2 torr.
        /// </summary>
        /// <param name="pressureNondimensionalized">
        /// The pressure nondimensionalized.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
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
        public static double DegreeCelsius2Nondimensionalized(double temperatureInCelsius)
        {
            return DegreeCelsius2Kelvin(temperatureInCelsius) / AbsoluteZeroInKelvin;
        }

        /// <summary>
        /// The nondimensionalized 2 kelvin.
        /// </summary>
        /// <param name="nondimensionalizedTemperature">
        /// The nondimensionalized temperature.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double Nondimensionalized2Kelvin(double nondimensionalizedTemperature)
        {
            return nondimensionalizedTemperature * AbsoluteZeroInKelvin;
        }

        /// <summary>
        /// The dalton to ppm.
        /// </summary>
        /// <param name="massInDalton">
        /// The mass in dalton.
        /// </param>
        /// <param name="baseMassInDalton">
        /// The base mass in dalton.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double DaltonToPpm(double massInDalton, double baseMassInDalton)
        {
            return massInDalton / baseMassInDalton * 1000000;
        }

        /// <summary>
        /// The ims scan number to drift time in ms.
        /// </summary>
        /// <param name="scanNumber">
        /// The scan number.
        /// </param>
        /// <param name="scanWidthInSeconds
        /// ">
        /// The scan width in seconds.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double ImsScanNumberToDriftTimeInMs(double scanNumber, double scanWidthInSeconds)
        {
            return scanWidthInSeconds * scanNumber * 1000;
        }

        /// <summary>
        /// The drift time in ms to ims scan number.
        /// </summary>
        /// <param name="driftTimeInMs">
        /// The drift time in ms.
        /// </param>
        /// <param name="scanWidthInSeconds">
        /// The scan width in seconds.
        /// </param>
        /// <param name="totalScans">
        /// The total Scans.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static int DriftTimeInMsToNearestImsScanNumber(double driftTimeInMs, double scanWidthInSeconds, int totalScans)
        {
            int scan = (int)Math.Round(driftTimeInMs / scanWidthInSeconds / 1000);
            if (scan < 1)
            {
                return 1;
            }
            else if (scan > totalScans)
            {
                return totalScans;
            }
            else
            {
                return scan;     
            }
        }
    }
}
