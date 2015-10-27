// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
namespace ImsInformed.Util
{
    using System;

    /// <summary>
    /// The unit conversion.
    /// </summary>
    internal class Metrics
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
        /// The room temperature.
        /// </summary>
        public const double LoschmidtConstant = 2.6867805e25;

        public const double ElectronVolt = 1.60217646e-19;

        /// <summary>
        /// The N0.
        /// </summary>
        public const double BoltzmannConstant = 1.3806488e-23;

        /// <summary>
        /// The standard ims pressure in TORR.
        /// </summary>
        public const double StandardImsPressureInTorr = 4;

        /// <summary>
        /// The pascal unit TORR.
        /// </summary>
        public const double PascalPerTorr = 133.322368;

        /// <summary>
        /// The pascal unit TORR.
        /// </summary>
        public const double GramPerDalton = 1.66053892e-24;

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

        public static double Dalton2Gram(double massInDalton)
        {
            return massInDalton * GramPerDalton;
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
