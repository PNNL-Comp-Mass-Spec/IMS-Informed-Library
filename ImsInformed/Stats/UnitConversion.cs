using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Stats
{
    public class UnitConversion
    {
        public static double Torr2Pascal(double pressureInTorr)
        {
            return 133.322368 * pressureInTorr;
        }

        public static double Torr2Nondimensionalized(double pressureInTorr)
        {
            return (133.322368 * pressureInTorr) / 101325;
        }

        public static double DegreeCelsius2Kelvin(double temperature)
        {
            return 273.15 + temperature;
        }

        public static double DegreeCelsius2Nondimensionalized(double temperature)
        {
            return (273.15 + temperature) / 273.15;
        }
    }
}
