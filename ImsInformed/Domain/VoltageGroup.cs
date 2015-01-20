// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VoltageGroup.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the VoltageGroup type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain
{
    using System;

    using ImsInformed.Stats;

    using MultiDimensionalPeakFinding.PeakDetection;

    /// <summary>
    /// Represents a group of adjacent frames whose drift tube voltages are similar.
    /// </summary>
    public class VoltageGroup : ICloneable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoltageGroup"/> class.
        /// </summary>
        /// <param name="meanVoltageInVolts">
        /// The mean voltage in volts.
        /// </param>
        /// <param name="varianceVoltage">
        /// The variance voltage.
        /// </param>
        /// <param name="count">
        /// The count.
        /// </param>
        private VoltageGroup(double meanVoltageInVolts, double varianceVoltage, int count)
        {
            this.MeanVoltageInVolts = meanVoltageInVolts;
            this.VarianceVoltage = varianceVoltage;
            this.AccumulationCount = count;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoltageGroup"/> class.
        /// </summary>
        /// <param name="frameNumber">
        /// The frame number.
        /// </param>
        public VoltageGroup(int frameNumber)
        {
            this.FirstFrameNumber = frameNumber;
            this.MeanVoltageInVolts = 0;
            this.VarianceVoltage = 0;
            this.MeanTemperatureNondimensionalized = 0;
            this.VarianceTemperature = 0;
            this.AccumulationCount = 0;
            this.MeanPressureNondimensionalized = 0;
            this.AverageTofWidthInSeconds = 0;
            this.VariancePressure = 0;
            this.BestFeature = null;
            this.BestScore = 0;
        }

        /// <summary>
        /// Gets the first frame number.
        /// </summary>
        public int FirstFrameNumber {get; private set;}

        /// <summary>
        /// Gets the mean voltage in volts.
        /// </summary>
        public double MeanVoltageInVolts { get; private set; }

        /// <summary>
        /// Gets the variance voltage.
        /// </summary>
        public double VarianceVoltage { get; private set; }

        /// <summary>
        /// Gets the accumulation count.
        /// </summary>
        public int AccumulationCount { get; private set; }

        /// <summary>
        /// Gets the mean temperature nondimensionalized.
        /// </summary>
        public double MeanTemperatureNondimensionalized { get; private set; }

        /// <summary>
        /// Gets the variance temperature.
        /// </summary>
        public double VarianceTemperature { get; private set; }

        /// <summary>
        /// Gets the mean pressure nondimensionalized.
        /// </summary>
        public double MeanPressureNondimensionalized { get; private set; }

        /// <summary>
        /// Gets the variance pressure.
        /// </summary>
        public double VariancePressure { get; private set; }

        /// <summary>
        /// Gets the average TOF width.
        /// </summary>
        public double AverageTofWidthInSeconds {get; private set; }

        /// <summary>
        /// Gets or sets the BestScore.
        /// stores ion detection as the following result 
        /// </summary>
        public double BestScore { get; set; }

        /// <summary>
        /// Gets or sets the confidence score.
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Gets or sets the best feature.
        /// </summary>
        public FeatureBlob BestFeature { get; set; }

        /// <summary>
        /// Gets or sets the fit point.
        /// </summary>
        public ContinuousXYPoint FitPoint { get; set; }

        /// <summary>
        /// The add voltage.
        /// </summary>
        /// <param name="newVoltage">
        /// The new voltage.
        /// </param>
        /// <param name="newPressure">
        /// The new pressure.
        /// </param>
        /// <param name="newTemperature">
        /// The new temperature.
        /// </param>
        /// <param name="newTOFWidth">
        /// The new TOF width.
        /// </param>
        public void AddVoltage(double newVoltage, double newPressure, double newTemperature, double newTOFWidth)
        {
            this.AccumulationCount++;

            // Calculate the mean from prev mean
            double newMeanVoltage = (this.MeanVoltageInVolts * (this.AccumulationCount - 1) + newVoltage)
                                    / this.AccumulationCount;

            // Calculate the std.dev from previous mean and std deviation
            this.VarianceVoltage = ((this.AccumulationCount - 1) * this.VarianceVoltage + (newVoltage - newMeanVoltage) * (newVoltage - this.MeanVoltageInVolts)) / this.AccumulationCount;
            this.MeanVoltageInVolts = newMeanVoltage;

            // Accumulate temperature
            double newMeanTemperature = (this.MeanTemperatureNondimensionalized * (this.AccumulationCount - 1)
                                         + newTemperature) / this.AccumulationCount;
            this.VarianceTemperature = ((this.AccumulationCount - 1) * VarianceTemperature
                                        + (newTemperature - newMeanTemperature)
                                        * (newTemperature - this.MeanTemperatureNondimensionalized))
                                       / this.AccumulationCount;
            this.MeanTemperatureNondimensionalized = newMeanTemperature;

            // Accumulate pressure
            double newMeanPressure = (this.MeanPressureNondimensionalized * (this.AccumulationCount - 1) + newPressure)/this.AccumulationCount;
            this.VariancePressure = ((this.AccumulationCount - 1) * VariancePressure + (newPressure - newMeanPressure) * (newPressure - this.MeanPressureNondimensionalized)) / this.AccumulationCount;
            this.MeanPressureNondimensionalized = newMeanPressure;

            // Accumulate TOF scans
            this.AverageTofWidthInSeconds = (this.AverageTofWidthInSeconds * (this.AccumulationCount - 1) + newTOFWidth)
                                   / this.AccumulationCount;
        }

        /// <summary>
        /// The add voltage dry run. Mainly used as a way to calculate the next standard
        /// deviation.
        /// </summary>
        /// <param name="newVoltage">
        /// The new voltage.
        /// </param>
        /// <returns>
        /// The <see cref="VoltageGroup"/>.
        /// </returns>
        public VoltageGroup AddVoltageDryRun(double newVoltage)
        {
            VoltageGroup mirrorVoltageGroup = (VoltageGroup)this.Clone();
            mirrorVoltageGroup.AddVoltage(newVoltage, 0, 0, 0);
            return mirrorVoltageGroup;
        }

        /// <summary>
        /// If new voltage causes a drastic change in standard deviation, the newVoltage won't be added
        /// and the method would return false
        /// </summary>
        /// <param name="newVoltage">
        /// The new voltage.
        /// </param>
        /// <param name="newPressure">
        /// The new pressure.
        /// </param>
        /// <param name="newTemperature">
        /// The new temperature.
        /// </param>
        /// <param name="newTOFWidth">
        /// The new TOF width.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool AddSimilarVoltage(double newVoltage, double newPressure, double newTemperature, double newTOFWidth)
        {
            bool success = this.VoltageGroupClassifier(newVoltage);
            if (success)
            {
                this.AddVoltage(newVoltage, newPressure, newTemperature, newTOFWidth);
            }

            return success;
        }

        /// <summary>
        /// The clone.
        /// </summary>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Clone()
        {
            VoltageGroup vg = new VoltageGroup(this.MeanVoltageInVolts, this.VarianceVoltage, this.AccumulationCount);
            return vg;
        }

        /// <summary>
        /// Estimate if the incoming voltage belongs to the current voltage group, using a custom classifier
        /// </summary>
        /// <param name="newVoltage">
        /// The new voltage.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool VoltageGroupClassifier(double newVoltage)
        {
            // guilty until proven innocent
            bool similarity = false;

            const double MinDifferentialVoltage = 5;
            
            // if first voltage, declare it as similar.
            if ((this.AccumulationCount == 0) ||

                // if the new voltage is within 3 standard deviation, declare it as a similar point.
                (Math.Abs(newVoltage - this.MeanVoltageInVolts) < 3 * Math.Sqrt(this.VarianceVoltage)) ||
 
                // if the new voltage is within min differential voltage, declare it as similar point.
                (Math.Abs(newVoltage - this.MeanVoltageInVolts) < MinDifferentialVoltage) || 
            
            // if the new voltage is not going to signaficantly alter the standard deviation, declare it as similar point
                (this.AddVoltageDryRun(newVoltage).VarianceVoltage < this.VarianceVoltage * 2))
            {
                similarity = true;
            }

            return similarity;
        }
    }
}
