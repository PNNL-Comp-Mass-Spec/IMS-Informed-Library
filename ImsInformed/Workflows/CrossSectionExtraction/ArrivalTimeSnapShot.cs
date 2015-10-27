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
namespace ImsInformed.Workflows.CrossSectionExtraction
{
    using System;

    /// <summary>
    /// Snapshot of arrival time.
    /// </summary>
    [Serializable]
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