using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
    using System.Diagnostics.Eventing.Reader;
    using System.Dynamic;

    using ImsInformed.Stats;

    using MultiDimensionalPeakFinding.PeakDetection;

    // Represents a group of ajacent frames whose drifttube voltages are similar.
    public class VoltageGroup : ICloneable
    {
        public int FirstFrameNumber {get; private set;}
        public double MeanVoltageInVolts { get; private set; }
        public double VarianceVoltage { get; private set; }
        public int AccumulationCount { get; private set; }
        public double MeanTemperatureNondimensionalized { get; private set; }
        public double VarianceTemperature { get; private set; }
        public double MeanPressureNondimensionalized { get; private set; }
        public double VariancePressure { get; private set; }

        // stores ion detection as the following result 
        public double BestScore { get; set; }
        public double ConfidenceScore { get; set; }
        public FeatureBlob BestFeature { get; set; }
        public ContinuousXYPoint FitPoint { get; set; }

        public VoltageGroup(int frameNumber)
        {
            this.FirstFrameNumber = frameNumber;
            this.MeanVoltageInVolts = 0;
            this.VarianceVoltage = 0;
            this.MeanTemperatureNondimensionalized = 0;
            VarianceTemperature = 0;
            AccumulationCount = 0;
            this.MeanPressureNondimensionalized = 0;
            VariancePressure = 0;
            this.BestFeature = null;
            this.BestScore = 0;
        }

        private VoltageGroup(double meanVoltageInVolts, double varianceVoltage, int count)
        {
            this.MeanVoltageInVolts = meanVoltageInVolts;
            this.VarianceVoltage = varianceVoltage;
            this.AccumulationCount = count;
        }

        public void AddVoltage(double newVoltage, double newPressure, double newTemperature)
        {
            this.AccumulationCount++;
            // Calculate the mean from prev mean
            double newMeanVoltage = (this.MeanVoltageInVolts * (this.AccumulationCount - 1) + newVoltage)/this.AccumulationCount;
            // Calculate the std.dev from previous mean and std deviation
            this.VarianceVoltage = ((this.AccumulationCount - 1) * this.VarianceVoltage + (newVoltage - newMeanVoltage) * (newVoltage - this.MeanVoltageInVolts)) / this.AccumulationCount;
            this.MeanVoltageInVolts = newMeanVoltage;

            // Accumulate temperature
            double newMeanTemperature = (this.MeanTemperatureNondimensionalized * (this.AccumulationCount - 1) + newTemperature)/this.AccumulationCount;
            this.VarianceTemperature = ((this.AccumulationCount - 1) * VarianceTemperature + (newTemperature - newMeanTemperature) * (newTemperature - this.MeanTemperatureNondimensionalized)) / this.AccumulationCount;
            this.MeanTemperatureNondimensionalized = newMeanTemperature;

            // Accumulate pressure
            double newMeanPressure = (this.MeanPressureNondimensionalized * (this.AccumulationCount - 1) + newPressure)/this.AccumulationCount;
            this.VariancePressure = ((this.AccumulationCount - 1) * VariancePressure + (newPressure - newMeanPressure) * (newPressure - this.MeanPressureNondimensionalized)) / this.AccumulationCount;
            this.MeanPressureNondimensionalized = newMeanPressure;
        }

        public VoltageGroup AddVoltageDryRun(double newVoltage)
        {
            VoltageGroup mirrorVG = (VoltageGroup)this.Clone();
            mirrorVG.AddVoltage(newVoltage, 0, 0);
            return mirrorVG;
        }

        // If new voltage causes a drastic change in standard deviation, the newVoltage won't be added
        // and the method would return force
        public bool AddSimilarVoltage(double newVoltage, double newPressure, double newTemperature)
        {
            bool success = this.VoltageGroupClassifier(newVoltage);
            if (success)
                this.AddVoltage(newVoltage,  newPressure, newTemperature);
            return success;
        }

        public object Clone()
        {
            VoltageGroup vg = new VoltageGroup(this.MeanVoltageInVolts, this.VarianceVoltage, this.AccumulationCount);
            return vg;
        }

        // Estimate if the incoming voltage belongs to the current voltage group, using a custom classifier
        private bool VoltageGroupClassifier(double newVoltage)
        {
            // guilty until proven innocent
            bool similarity = false;

            const double MinDifferentialVoltage = 5;
            
            // if first voltage, declare it as similar.
            if ((this.AccumulationCount == 0) ||
            
            // if the new voltage is within 3 standard deviation, declare it as a similar point.
            (Math.Abs(newVoltage - this.MeanVoltageInVolts) < 3*Math.Sqrt(this.VarianceVoltage)) ||

            // if the new voltage is within min differential voltage, declare it as similar point.
            (Math.Abs(newVoltage - this.MeanVoltageInVolts) < MinDifferentialVoltage) || 
            
            // if the new voltage is not going to signaficantly alter the standard deviation, declare it as similar point
            (this.AddVoltageDryRun(newVoltage).VarianceVoltage < this.VarianceVoltage * 2)
                )
            {
                similarity = true;
            }
            return similarity;
        }
    }
}
