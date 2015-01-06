using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
    using ImsInformed.Parameters;
    using ImsInformed.Stats;

    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    // VoltageSeparatedAccumulatedXICs, where Accumulated XIC stands for a 3D XIC accumulated over the frame axis.
    public class VoltageSeparatedAccumulatedXICs : Dictionary<VoltageGroup, ExtractedIonChromatogram>
    {
        // The input UIMF file needs to be a UIMF file created by direct-injection
        // IMS, with different drifttube voltages at different frames.
        // This constructor intelligently group voltages together by observing
        // sharp changes in running voltage standard deviation
        public VoltageSeparatedAccumulatedXICs(DataReader uimfReader, double targetMz, InformedParameters informedParams) : base()
        {
            int frameNum = uimfReader.GetGlobalParams().NumFrames;
            
            VoltageGroup currentVoltageGroup = new VoltageGroup(1);
            for (int i = 1; i < frameNum; i++)
            {
                FrameParams param = uimfReader.GetFrameParams(i);
                double driftTubeVoltageInVolts = param.GetValueDouble(FrameParamKeyType.FloatVoltage);
                double driftTubeTemperatureNondimensionalized = UnitConversion.DegreeCelsius2Nondimensionalized(param.GetValueDouble(FrameParamKeyType.AmbientTemperature));
                double driftTubePressureNondimensionalized = UnitConversion.Torr2Nondimensionalized(param.GetValueDouble(FrameParamKeyType.PressureBack));
                if (driftTubeVoltageInVolts <= 0)
                    throw new InvalidOperationException("Floating voltage is recorded as 0 in the uimf file, "
                                                        + "try run the newer version of agilent .d folder conveter");
                List<IntensityPoint> XIC = uimfReader.GetXic(targetMz, informedParams.MassToleranceInPpm, 
                    i, i, 0, param.Scans, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

                // For non empty XICs, add to the VXIC
                if (XIC.Count != 0)
                {
                    ExtractedIonChromatogram extractedIonChromatogram = new ExtractedIonChromatogram(XIC, uimfReader, i, targetMz);
                    bool similarVoltage = currentVoltageGroup.AddSimilarVoltage(driftTubeVoltageInVolts, driftTubePressureNondimensionalized, driftTubeTemperatureNondimensionalized);
                    // And when a new but unsimilar voltage appears 
                
                    if (!similarVoltage)
                    {
                        currentVoltageGroup = new VoltageGroup(i);
                        currentVoltageGroup.AddVoltage(driftTubeVoltageInVolts, driftTubePressureNondimensionalized, driftTubeTemperatureNondimensionalized);
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
            }
        }
    }
}
