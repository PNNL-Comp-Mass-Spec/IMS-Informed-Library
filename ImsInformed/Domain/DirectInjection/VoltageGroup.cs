// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VoltageGroup.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the VoltageGroup type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DirectInjection
{
    using System;

    using ImsInformed.Scoring;
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
            this.FrameAccumulationCount = count;
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
            this.FrameAccumulationCount = 0;
            this.MeanPressureNondimensionalized = 0;
            this.AverageTofWidthInSeconds = 0;
            this.VariancePressure = 0;
            this.BestFeature = null;
            
            this.BestFeatureScores.IntensityScore = 0;
            this.BestFeatureScores.IsotopicScore = 0;
            this.BestFeatureScores.PeakShapeScore = 0;
        }

        /// <summary>
        /// Gets the first frame number.
        /// </summary>
        public int FirstFrameNumber { get; private set; }

        /// <summary>
        /// Gets the first frame number.
        /// </summary>
        public int LastFrameNumber
        {
            get
            {
                return this.FirstFrameNumber + this.FrameAccumulationCount - 1;
            }
        }

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
        public int FrameAccumulationCount { get; private set; }

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
        /// Gets or sets the AverageCandidateTargetScores.
        /// stores ion detection as the following result 
        /// </summary>
        public FeatureScoreHolder BestFeatureScores;

        /// <summary>
        /// Gets or sets the confidence score.
        /// </summary>
        public double VoltageGroupScore { get; set; }

        /// <summary>
        /// Gets or sets the best feature.
        /// </summary>
        public StandardImsPeak BestFeature { get; set; }

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
            this.FrameAccumulationCount++;

            // Calculate the mean from prev mean
            double newMeanVoltage = (this.MeanVoltageInVolts * (this.FrameAccumulationCount - 1) + newVoltage)
                                    / this.FrameAccumulationCount;

            // Calculate the std.dev from previous mean and std deviation
            this.VarianceVoltage = ((this.FrameAccumulationCount - 1) * this.VarianceVoltage + (newVoltage - newMeanVoltage) * (newVoltage - this.MeanVoltageInVolts)) / this.FrameAccumulationCount;
            this.MeanVoltageInVolts = newMeanVoltage;

            // Accumulate temperature
            double newMeanTemperature = (this.MeanTemperatureNondimensionalized * (this.FrameAccumulationCount - 1)
                                         + newTemperature) / this.FrameAccumulationCount;
            this.VarianceTemperature = ((this.FrameAccumulationCount - 1) * this.VarianceTemperature
                                        + (newTemperature - newMeanTemperature)
                                        * (newTemperature - this.MeanTemperatureNondimensionalized))
                                       / this.FrameAccumulationCount;
            this.MeanTemperatureNondimensionalized = newMeanTemperature;

            // Accumulate pressure
            double newMeanPressure = (this.MeanPressureNondimensionalized * (this.FrameAccumulationCount - 1) + newPressure)/this.FrameAccumulationCount;
            this.VariancePressure = ((this.FrameAccumulationCount - 1) * this.VariancePressure + (newPressure - newMeanPressure) * (newPressure - this.MeanPressureNondimensionalized)) / this.FrameAccumulationCount;
            this.MeanPressureNondimensionalized = newMeanPressure;

            // Accumulate TOF scans
            this.AverageTofWidthInSeconds = (this.AverageTofWidthInSeconds * (this.FrameAccumulationCount - 1) + newTOFWidth)
                                   / this.FrameAccumulationCount;
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
            bool newVoltageGroupFlag = this.VoltageGroupClassifier(newVoltage);
            if (newVoltageGroupFlag)
            {
                this.AddVoltage(newVoltage, newPressure, newTemperature, newTOFWidth);
            }

            return newVoltageGroupFlag;
        }

        /// <summary>
        /// The clone.
        /// </summary>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Clone()
        {
            VoltageGroup vg = new VoltageGroup(this.MeanVoltageInVolts, this.VarianceVoltage, this.FrameAccumulationCount);
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
            if ((this.FrameAccumulationCount == 0) ||

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
