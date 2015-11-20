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
namespace ImsInformed.Domain.DirectInjection
{
    using System;
    using System.Collections.Generic;

    using ImsInformed.Util;

    using UIMFLibrary;

    /// <summary>
    /// VoltageSeparatedAccumulatedXICs, where Accumulated XIC stands for a 3D XIC accumulated over the frame axis.
    /// </summary>
    internal class VoltageSeparatedAccumulatedXiCs : Dictionary<VoltageGroup, ImsDataWindow>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoltageSeparatedAccumulatedXiCs"/> class. 
        /// The input UIMF file needs to be a UIMF file created by direct-injection
        /// IMS, with different drift tube voltages at different frames.
        /// This constructor intelligently group voltages together by observing
        /// sharp changes in running voltage standard deviation. Only the XIC around
        /// targeted MZ would be accumulated. 
        /// </summary>
        /// <param name="uimfReader">
        /// The UIMF reader.
        /// </param>
        /// <param name="targetMz">
        /// The Target MZ.
        /// </param>
        /// <param name="massToleranceInPpm">
        /// The mass tolerance in PPM.
        /// </param>
        /// <param name="normalizedTargetDriftTimeInMs">
        /// The Target drift time.
        /// </param>
        /// <param name="driftTimeToleranceInMs">
        /// The drift time tolerance in milliseconds.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public VoltageSeparatedAccumulatedXiCs(DataReader uimfReader, double targetMz, double massToleranceInPpm, double normalizedTargetDriftTimeInMs, double driftTimeToleranceInMs)
        {
            bool noVoltageGroupsYet = true;
            int frameNum = uimfReader.GetGlobalParams().NumFrames;
            ImsDataWindow currentXIC = new ImsDataWindow();
            VoltageGroup currentVoltageGroup = new VoltageGroup(1, frameNum);
            for (int i = 1; i <= frameNum; i++)
            {
                FrameParams param = uimfReader.GetFrameParams(i);
                double driftTubeVoltageInVolts = param.GetValueDouble(FrameParamKeyType.FloatVoltage) / 100 * FakeUIMFReader.DriftTubeLengthInCentimeters;
                double driftTubeTemperatureNondimensionalized = Metrics.DegreeCelsius2Nondimensionalized(param.GetValueDouble(FrameParamKeyType.AmbientTemperature));
                double driftTubePressureNondimensionalized = Metrics.Torr2Nondimensionalized(param.GetValueDouble(FrameParamKeyType.PressureBack));
                double tofWidthInSeconds = param.GetValueDouble(FrameParamKeyType.AverageTOFLength) / 1000000000;
                if (driftTubeVoltageInVolts <= 0)
                {
                    throw new InvalidOperationException(
                        "Floating voltage is recorded as 0 in the uimf file, "
                        + "try run the newer version of agilent .d folder conveter");
                }

                // Add XICs to the VXICs
                ImsDataWindow imsDataWindow;
                if (normalizedTargetDriftTimeInMs > 0 || driftTimeToleranceInMs > 0)
                {
                    double expectedDriftTime = IMSUtil.DeNormalizeDriftTime(
                        normalizedTargetDriftTimeInMs,
                        Metrics.Nondimensionalized2Torr(driftTubePressureNondimensionalized),
                        Metrics.Nondimensionalized2Kelvin(driftTubeTemperatureNondimensionalized));
                    imsDataWindow = new ImsDataWindow(uimfReader, i, targetMz, massToleranceInPpm, expectedDriftTime, driftTimeToleranceInMs);
                }
                else
                {
                    imsDataWindow = new ImsDataWindow(uimfReader, i, targetMz, massToleranceInPpm);
                }
                
                bool similarVoltage = currentVoltageGroup.AddSimilarVoltage(driftTubeVoltageInVolts, driftTubePressureNondimensionalized, driftTubeTemperatureNondimensionalized, tofWidthInSeconds);
                
                // And when a new but unsimilar voltage appears 
                if (!similarVoltage)
                {
                    if (!noVoltageGroupsYet)
                    {
                        this.Add(currentVoltageGroup, currentXIC);
                    }

                    currentVoltageGroup = new VoltageGroup(i, frameNum);
                    currentVoltageGroup.AddVoltage(driftTubeVoltageInVolts, driftTubePressureNondimensionalized, driftTubeTemperatureNondimensionalized, tofWidthInSeconds);
                    currentXIC = imsDataWindow;
                    noVoltageGroupsYet = false;
                }
                else
                {
                    currentXIC += imsDataWindow;
                }
            }

            this.Add(currentVoltageGroup, currentXIC);

            // Average all the XICs to have the intensity range of a single frame.
            foreach (var voltageGroup in this.Keys)
            {
                foreach (var item in this[voltageGroup].IntensityPoints)   
                {
                    item.Intensity /= voltageGroup.FrameAccumulationCount;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoltageSeparatedAccumulatedXiCs"/> class. 
        /// The input UIMF file needs to be a UIMF file created by direct-injection
        /// IMS, with different drift tube voltages at different frames.
        /// This constructor intelligently group voltages together by observing
        /// sharp changes in running voltage standard deviation. Only the XIC around
        /// targeted MZ would be accumulated. 
        /// </summary>
        /// <param name="uimfReader">
        /// The UIMF reader.
        /// </param>
        /// <param name="targetMz">
        /// The Target M/Z.
        /// </param>
        /// <param name="massToleranceInPpm">
        /// The mass tolerance in ppm.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public VoltageSeparatedAccumulatedXiCs(DataReader uimfReader, double targetMz, double massToleranceInPpm) : this(uimfReader, targetMz, massToleranceInPpm, -1, -1)
        {
        }
    }
}
