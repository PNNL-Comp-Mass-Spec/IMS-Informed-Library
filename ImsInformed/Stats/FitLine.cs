// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FitLine.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The fit line.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Stats
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The fit line.
    /// </summary>
    public class FitLine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FitLine"/> class.
        /// </summary>
        /// <param name="fitPoints">
        /// The points to calculate the fit line from
        /// </param>
        /// <param name="outlierThreshold">
        /// The cook's distance threshold for a point to be identified as outliers.
        /// </param>
        public FitLine(HashSet<ContinuousXYPoint> fitPoints, double outlierThreshold = 3)
        {
            this.OutlierThreshold = outlierThreshold;
            this.PointCollection = fitPoints;
            this.LeastSquaresFitLinear(fitPoints);
        }

        /// <summary>
        /// Gets or sets the point collection.
        /// </summary>
        public HashSet<ContinuousXYPoint> PointCollection { get; protected set; }

        /// <summary>
        /// Gets or sets the intercept.
        /// </summary>
        public double Intercept { get; protected set; }

        /// <summary>
        /// Gets or sets the slope.
        /// </summary>
        public double Slope { get; protected set; }

        /// <summary>
        /// Gets or sets the MSE.
        /// </summary>
        public double MSE { get; protected set; }

        /// <summary>
        /// Gets or sets the RSquared.
        /// </summary>
        public double RSquared { get; protected set; }

        /// <summary>
        /// Gets or sets the outlier threshold.
        /// </summary>
        public double OutlierThreshold { get; protected set; }

        /// <summary>
        /// Computes fit line for potential voltage group and writes
        /// </summary>
        /// <param name="xyPoints">
        /// The xy points.
        /// </param>
        public void LeastSquaresFitLinear(IEnumerable<ContinuousXYPoint> xyPoints)
        {
            int count = 0;
            double meanX = 0;
            double meanY = 0;
            double meanXSquared = 0;
            double meanXY = 0;
            foreach (ContinuousXYPoint point in xyPoints)
            {
                count++;
                meanX += point.x;
                meanY += point.y;
                meanXSquared += point.x * point.x;
                meanXY += point.x * point.y;
            }
            meanX = meanX / count;
            meanY = meanY / count;
            meanXY = meanXY / count;
            meanXSquared = meanXSquared / count;

            this.Slope = (meanXY - meanX * meanY) / (meanXSquared - meanX * meanX);
            this.Intercept = meanY - this.Slope * meanX;
            this.MSE = this.CalculateMSE();
            this.RSquared = this.CalculateRSquared();
            this.CalculateCooksDistances();
        }

        /// <summary>
        /// The compute residuel.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        private double ComputeResiduel(ContinuousXYPoint point)
        {
            if (this.PointCollection.Contains(point))
            {
                return Math.Abs(this.ModelPredict(point.x) - point.y);
            }
            else
            {
                throw new ArgumentException("Point given is not inside Fitline point list");
            }
        }

        /// <summary>
        /// Return the predicted Y at given X
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <returns>
        /// the predicted Y at given X <see cref="double"/>.
        /// </returns>
        public double ModelPredict(double x)
        {
            return Slope * x + Intercept;    
        }

        // Calculate Cook's distance for all points
        private void CalculateCooksDistances()
        {
            int pointsCount = this.PointCollection.Count;
            // calcaulate average X
            double meanX = 0;
            foreach (ContinuousXYPoint point in this.PointCollection)
            {
                meanX += point.x;
            }
            meanX /= pointsCount;

            // Calculate sum of squares / SSx.
            double SSx = 0;
            foreach (ContinuousXYPoint point in this.PointCollection)
            {
                SSx += (point.x - meanX) * (point.x - meanX);
            }

            // hat value = 1/n + (xi - x_bar)^2 / SSx
            foreach (ContinuousXYPoint point in this.PointCollection)
            {
                point.CooksD = CooksDistance(point, SSx, meanX, pointsCount);
                if (point.CooksD >= this.OutlierThreshold)
                {
                    point.IsOutlier = true;
                }
            }
        }

        /// <summary>
        /// The cooks distance.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="ssh">
        /// The ssh.
        /// </param>
        /// <param name="meanX">
        /// The mean x.
        /// </param>
        /// <param name="pointsCount">
        /// The points count.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private double CooksDistance(ContinuousXYPoint point, double ssh, double meanX, int pointsCount)
        {
            double distance;
            double residuel = this.ComputeResiduel(point);
            double p = 3;
            double mse = this.CalculateMSE();
            distance = residuel * residuel / p / mse;
            // Get the Hii from the hat matrix
            double hii = 1.0 / pointsCount + (point.x - meanX) * (point.x - meanX) / ssh;
            distance = distance * hii / ((1 - hii) * (1 - hii));
            return distance;
        }

        // compute the mean square error
        private double CalculateMSE()
        {
            double sum = 0;
            foreach (ContinuousXYPoint point in this.PointCollection)
            {
                double residuel = this.ComputeResiduel(point);
                sum += residuel * residuel;
            }

            return sum / this.PointCollection.Count;
        }

        // Calculate R-square(Coefficient of determination)
        private double CalculateRSquared()
        {
            // Calculate average Y
            double avgY = 0;
            foreach (ContinuousXYPoint point in this.PointCollection)
            {
                avgY += point.y;
            }
            
            avgY /= this.PointCollection.Count;

            // Calculate explained sum of squares
            double SSreg = 0;
            double SStot = 0;
            foreach (ContinuousXYPoint point in this.PointCollection)
            {
                double residuel1 = this.ComputeResiduel(point);
                double residuel2 = Math.Abs(avgY - point.y);

                SSreg += residuel1 * residuel1;
                SStot += residuel2 * residuel2;
            }

            double rSquared = 1 - SSreg / SStot;

            return rSquared;
        }
    }
}  