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
namespace ImsInformed.Statistics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DeconTools.Backend.Utilities;

    /// <summary>
    /// The fit FitLine.
    /// </summary>
    internal class FitLine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FitLine"/> class.
        /// </summary>
        /// <param name="fitPoints">
        /// The points to calculate the fit FitLine from
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
        /// Return the predicted Y at given X
        /// </summary>
        /// <param name="x">
        /// The X.
        /// </param>
        /// <returns>
        /// the predicted Y at given X <see cref="double"/>.
        /// </returns>
        public double ModelPredictX2Y(double x)
        {
            return (this.Slope * x) + this.Intercept;
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
        public double ModelPredictY2X(double y)
        {
            return (y - this.Intercept) / this.Slope;
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
        /// Computes fit FitLine for potential voltage group and writes
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
                return Math.Abs(this.ModelPredictX2Y(point.X) - point.Y);
            }
            else
            {
                throw new ArgumentException("Point given is not inside Fitline point list");
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