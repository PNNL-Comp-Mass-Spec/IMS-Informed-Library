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
    using System.Linq;

    using DeconTools.Backend.Utilities;

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
        public FitLine(IEnumerable<ContinuousXYPoint> fitPoints)
        {
            this.MSE = 0;
            this.RSquared = 0;
            this.Slope = 0;
            this.Intercept = 0;
            this.OutlierCollection = new HashSet<ContinuousXYPoint>();
            this.FitPointCollection = new HashSet<ContinuousXYPoint>();
            this.LeastSquaresFitLinear(fitPoints);
        }

        /// <summary>
        /// Gets or sets the point collection.
        /// </summary>
        public HashSet<ContinuousXYPoint> FitPointCollection { get; private set; }

        /// <summary>
        /// Gets or sets the point collection.
        /// </summary>
        public HashSet<ContinuousXYPoint> OutlierCollection { get; private set; }

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
        /// Computes fit line for potential voltage group and writes
        /// </summary>
        /// <param name="xyPoints">
        /// The xy points.
        /// </param>
        private void LeastSquaresFitLinear(IEnumerable<ContinuousXYPoint> xyPoints)
        {
            int count = 0;
            double meanX = 0;
            double meanY = 0;
            double meanXSquared = 0;
            double meanXY = 0;
            foreach (ContinuousXYPoint point in xyPoints)
            {
                this.FitPointCollection.Add(point.Clone());
                count++;
                meanX += point.X;
                meanY += point.Y;
                meanXSquared += point.X * point.X;
                meanXY += point.X * point.Y;
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
            if (this.FitPointCollection.Contains(point))
            {
                return Math.Abs(this.ModelPredict(point.X) - point.Y);
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
        /// The X.
        /// </param>
        /// <returns>
        /// the predicted Y at given X <see cref="double"/>.
        /// </returns>
        public double ModelPredict(double x)
        {
            return Slope * x + Intercept;    
        }

        /// <summary>
        /// The remove outliers above threshold.
        /// </summary>
        /// <param name="CooksDThreshold">
        /// The cooks d threshold.
        /// </param>
        /// <param name="minFitPoints">
        /// The min fit points.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public int RemoveOutliersAboveThreshold(double CooksDThreshold, int minFitPoints)
        {
            if (this.FitPointCollection.Count > minFitPoints)
            {
                int discretion = this.FitPointCollection.Count - minFitPoints;
                if (this.FitPointCollection == null)
                {
                    throw new InvalidOperationException("Fitline has not been created");
                }

                foreach (var point in this.FitPointCollection)
                {
                    if (discretion < 1)
                    {
                        break;
                    }
                    else
                    {
                        if (point.CooksD > CooksDThreshold)
                        {
                            this.OutlierCollection.Add(point);
                            discretion--;
                        }
                    }
                }

                foreach (var point in this.OutlierCollection)
                {
                    if (this.FitPointCollection.Contains(point)) 
                    {
                        this.FitPointCollection.Remove(point);
                    }
                }

                this.LeastSquaresFitLinear(this.FitPointCollection);
            }

            return this.FitPointCollection.Count;
        }

        /// <summary>
        /// The remove outlier with highest cook distance.
        /// </summary>
        public int RemoveOutlierWithHighestCookDistance(int minFitPoints)
        {
            int size = this.FitPointCollection.Count;
            if (size >= minFitPoints)
            {
                if (size != 0)
                {
                    ContinuousXYPoint highestPoint = this.FitPointCollection.First();
                    double highestCookD = highestPoint.CooksD;
                    foreach (var point in this.FitPointCollection)
                    {
                        if (point.CooksD > highestCookD)
                        {
                            highestPoint = point;
                            highestCookD = point.CooksD;
                        }
                    }

                    this.FitPointCollection.Remove(highestPoint);
                    this.OutlierCollection.Add(highestPoint);
                }

                this.LeastSquaresFitLinear(this.FitPointCollection);
            }
            return this.FitPointCollection.Count;
        }

        /// <summary>
        /// Calculate Cook's distance for all points
        /// </summary>
        private void CalculateCooksDistances()
        {
            int pointsCount = this.FitPointCollection.Count;
            // calcaulate average X
            double meanX = 0;
            foreach (ContinuousXYPoint point in this.FitPointCollection)
            {
                meanX += point.X;
            }
            meanX /= pointsCount;

            // Calculate sum of squares / SSx.
            double SSx = 0;
            foreach (ContinuousXYPoint point in this.FitPointCollection)
            {
                SSx += (point.X - meanX) * (point.X - meanX);
            }

            // hat value = 1/n + (xi - x_bar)^2 / SSx
            foreach (ContinuousXYPoint point in this.FitPointCollection)
            {
                point.CooksD = CooksDistance(point, SSx, meanX, pointsCount);
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
        /// The mean X.
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
            double hii = 1.0 / pointsCount + (point.X - meanX) * (point.X - meanX) / ssh;
            distance = distance * hii / ((1 - hii) * (1 - hii));
            return distance;
        }

        // compute the mean square error
        private double CalculateMSE()
        {
            double sum = 0;
            foreach (ContinuousXYPoint point in this.FitPointCollection)
            {
                double residuel = this.ComputeResiduel(point);
                sum += residuel * residuel;
            }

            return sum / this.FitPointCollection.Count;
        }

        /// <summary>
        /// Calculate R-square(Coefficient of determination)
        /// </summary>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private double CalculateRSquared()
        {
            // Calculate average Y
            double avgY = 0;
            foreach (ContinuousXYPoint point in this.FitPointCollection)
            {
                avgY += point.Y;
            }
            
            avgY /= this.FitPointCollection.Count;

            // Calculate explained sum of squares
            double SSreg = 0;
            double SStot = 0;
            foreach (ContinuousXYPoint point in this.FitPointCollection)
            {
                double residuel1 = this.ComputeResiduel(point);
                double residuel2 = Math.Abs(avgY - point.Y);

                SSreg += residuel1 * residuel1;
                SStot += residuel2 * residuel2;
            }

            double rSquared = 1 - SSreg / SStot;

            return rSquared;
        }
    }
}  