// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VoltageSeparatedAccumulatedXICs.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the VoltageSeparatedAccumulatedXICs type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DirectInjection
{
    using System;
    using System.Collections.Generic;

    using ImsInformed.Util;

    using UIMFLibrary;

    /// <summary>
    /// VoltageSeparatedAccumulatedXICs, where Accumulated XIC stands for a 3D XIC accumulated over the frame axis.
    /// </summary>
    public class VoltageSeparatedAccumulatedXiCs : Dictionary<VoltageGroup, ExtractedIonChromatogram>
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
            int frameNum = uimfReader.GetGlobalParams().NumFrames;
            
            VoltageGroup currentVoltageGroup = new VoltageGroup(1);
            for (int i = 1; i <= frameNum; i++)
            {
                FrameParams param = uimfReader.GetFrameParams(i);
                double driftTubeVoltageInVolts = param.GetValueDouble(FrameParamKeyType.FloatVoltage);
                double driftTubeTemperatureNondimensionalized = UnitConversion.DegreeCelsius2Nondimensionalized(param.GetValueDouble(FrameParamKeyType.AmbientTemperature));
                double driftTubePressureNondimensionalized = UnitConversion.Torr2Nondimensionalized(param.GetValueDouble(FrameParamKeyType.PressureBack));
                double tofWidthInSeconds = param.GetValueDouble(FrameParamKeyType.AverageTOFLength) / 1000000000;
                if (driftTubeVoltageInVolts <= 0)
                {
                    throw new InvalidOperationException(
                        "Floating voltage is recorded as 0 in the uimf file, "
                        + "try run the newer version of agilent .d folder conveter");
                }

                // Add XICs to the VXICs
                ExtractedIonChromatogram extractedIonChromatogram;
                if (normalizedTargetDriftTimeInMs > 0 || driftTimeToleranceInMs > 0)
                {
                    double expectedDriftTime = IMSUtil.DeNormalizeDriftTime(
                        normalizedTargetDriftTimeInMs,
                        UnitConversion.Nondimensionalized2Torr(driftTubePressureNondimensionalized),
                        UnitConversion.Nondimensionalized2Kelvin(driftTubeTemperatureNondimensionalized));
                    extractedIonChromatogram = new ExtractedIonChromatogram(uimfReader, i, targetMz, massToleranceInPpm, expectedDriftTime, driftTimeToleranceInMs);
                }
                else
                {
                    extractedIonChromatogram = new ExtractedIonChromatogram(uimfReader, i, targetMz, massToleranceInPpm);
                }
                
                bool similarVoltage = currentVoltageGroup.AddSimilarVoltage(driftTubeVoltageInVolts, driftTubePressureNondimensionalized, driftTubeTemperatureNondimensionalized, tofWidthInSeconds);
                
                // And when a new but unsimilar voltage appears 
                if (!similarVoltage)
                {
                    currentVoltageGroup = new VoltageGroup(i);
                    currentVoltageGroup.AddVoltage(driftTubeVoltageInVolts, driftTubePressureNondimensionalized, driftTubeTemperatureNondimensionalized, tofWidthInSeconds);
                    this.Add(currentVoltageGroup, extractedIonChromatogram);
                } 

                if (!this.ContainsKey(currentVoltageGroup)) 
                {
                    this.Add(currentVoltageGroup, extractedIonChromatogram);
                } 
                else 
                {
                    this[currentVoltageGroup] += extractedIonChromatogram;
                }
            }

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
