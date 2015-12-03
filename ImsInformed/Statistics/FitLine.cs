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
    using System.Security.Cryptography;

    /// <summary>
    /// The fit FitLine.
    /// </summary>
    internal abstract class FitLine
    {
        private FitlineState state;

        private readonly List<FitlinePoint> fitPointCollection;

        private readonly HashSet<FitlinePoint> outlierCollection;

        private double rSquared;

        private double mse;

        /// <summary>
        /// Initializes a new instance of the <see cref="FitLine"/> class.
        /// </summary>
        /// <param name="fitPoints">
        /// The points to calculate the fit FitLine from
        /// </param>
        /// <param name="outlierThreshold">
        /// The cook's distance threshold for a point to be identified as outliers.
        /// </param>
        public FitLine()
        {
            this.Mse = 0;
            this.RSquared = 0;
            this.Slope = 0;
            this.Intercept = 0;
            this.outlierCollection = new HashSet<FitlinePoint>();
            this.fitPointCollection = new List<FitlinePoint>();
            this.state = FitlineState.Observing;
        }

        public FitLine(IEnumerable<ContinuousXYPoint> initialPoints) : this()
        {
            this.AddPoints(initialPoints);
        }

        /// <summary>
        /// Gets or sets the point collection.
        /// </summary>
        public IEnumerable<FitlinePoint> FitPointCollection
        {
            get
            {
                return this.fitPointCollection;
            }
        }

        /// <summary>
        /// Gets or sets the point collection.
        /// </summary>
        public IEnumerable<FitlinePoint> OutlierCollection
        {
            get
            {
               return this.outlierCollection;
            }
        }

        /// <summary>
        /// Gets or sets the intercept.
        /// </summary>
        public double Intercept { get; private set; }

        /// <summary>
        /// Gets or sets the slope.
        /// </summary>
        public double Slope { get; private set; }

        /// <summary>
        /// Gets or sets the mean square error.
        /// </summary>
        public double Mse
        {
            get
            {
                if (this.state >= FitlineState.DiagnosticsComplete)
                {
                    return this.mse;
                }
                else
                {
                    throw new Exception("Cannot obtain MSE as diagnostics step was not completed");
                }
            }

            private set
            {
                this.mse = value;
            }
        }

        /// <summary>
        /// Gets or sets the RSquared.
        /// </summary>
        public double RSquared
        {
            get
            {
                if (this.state >= FitlineState.DiagnosticsComplete)
                {
                    return this.rSquared;
                }
                else
                {
                    throw new Exception("Cannot obtain R2 as diagnostics step was not completed");
                }
            }

            private set
            {
                this.rSquared = value;
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
        public double ModelPredictX2Y(double x)
        {
            if (this.state < FitlineState.ModelComplete)
            {
                throw new Exception("Cannot predict value with model, please run linear regression first.");
            }

            return (this.Slope * x) + this.Intercept;
        }

        public void AddPoint(ContinuousXYPoint point)
        {
            this.fitPointCollection.Add(new FitlinePoint(point));
            this.state = FitlineState.Observing;
        }

        public void ResetPoints(IEnumerable<ContinuousXYPoint> points)
        {
            this.fitPointCollection.Clear();
            this.AddPoints(points);
        }

        public void AddPoints(IEnumerable<ContinuousXYPoint> points)
        {
            foreach (var point in points)
            {
                this.AddPoint(point);
            }
        }

        public void RemovePoint(ContinuousXYPoint point)
        {
            this.fitPointCollection.Remove(new FitlinePoint(point));
            this.state = FitlineState.Observing;
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
        /// <exception cref="Exception">Cannot predict value with model, please run linear regression first.</exception>
        public double ModelPredictY2X(double y)
        {
            if (this.state < FitlineState.ModelComplete)
            {
                throw new Exception("Cannot predict value with model, please run linear regression first.");
            }

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
            if (this.state >= FitlineState.DiagnosticsComplete)
            {
                if (this.fitPointCollection.Count > minFitPoints)
                {
                    int discretion = this.fitPointCollection.Count - minFitPoints;
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
                                this.outlierCollection.Add(point);
                                discretion--;
                            }
                        }
                    }

                    foreach (var point in this.OutlierCollection)
                    {
                        if (this.FitPointCollection.Contains(point)) 
                        {
                            this.fitPointCollection.Remove(point);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Remove outlier only when linear regression already ran and diagnostics finished.");
            }

            return this.fitPointCollection.Count;
        }

        /// <summary>
        /// The remove outlier with highest cook distance.
        /// </summary>
        public int RemoveOutlierWithHighestCookDistance(int minFitPoints)
        {
            if (this.state >= FitlineState.DiagnosticsComplete)
            {
                int size = this.fitPointCollection.Count;
                if (size >= minFitPoints)
                {
                    if (size != 0)
                    {
                        FitlinePoint highestPoint = this.FitPointCollection.First();
                        double highestCookD = highestPoint.CooksD;
                        foreach (var point in this.FitPointCollection)
                        {
                            if (point.CooksD > highestCookD)
                            {
                                highestPoint = point;
                                highestCookD = point.CooksD;
                            }
                        }

                        this.RemovePoint(highestPoint.Point);
                        this.outlierCollection.Add(highestPoint);

                    }

                    this.PerformRegression(this.FitPointCollection);
                }
            }
            else
            {
                throw new Exception("Remove outlier without running linear regression finishing diagnostics.");
            }
            return this.outlierCollection.Count;
        }

        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        public void DiagnoseRegression(Func<FitlinePoint, double> weightFunc = null)
        {
            // Calculate diagnostic information
            if (this.state >= FitlineState.ModelComplete)
            {
                double mse = this.CalculateMSE();
                double r2 =  this.CalculateRSquared();
                this.Mse = mse;
                this.RSquared = r2;
                if (weightFunc != null)
                {
                    this.DiagnosePoints(weightFunc);
                }
                else
                {
                    this.DiagnosePoints(x => 1);
                }
                
                this.state = FitlineState.DiagnosticsComplete;
            }
            else
            {
                throw new Exception("Has to complete model first, cannot diagnose regression");
            }
        }

        /// <summary>
        /// Refit a group of points to the fit line.
        /// </summary>
        /// <param name="xyPoints">
        /// The xy points.
        /// </param>
        public virtual void PerformRegression(IEnumerable<FitlinePoint> xyPoints)
        {
            IList<FitlinePoint> fitlinePoints = xyPoints as IList<FitlinePoint> ?? xyPoints.ToList();
            this.LeastSquaresFitdLinear(fitlinePoints);

            this.fitPointCollection.Clear();
            this.fitPointCollection.AddRange(fitlinePoints);
            this.outlierCollection.Clear();
            this.state = FitlineState.ModelComplete;
        }

        /// <summary>
        /// Using current fits points stored in the fitline to initiate regression
        /// </summary>
        /// <param name="xyPoints">
        /// The xy points.
        /// </param>
        public virtual void PerformRegression()
        {
            this.LeastSquaresFitdLinear(this.FitPointCollection);
            this.state = FitlineState.ModelComplete;
        }
        
        /// <summary>
        /// Computes fit FitLine for potential voltage group and writes
        /// </summary>
        /// <param name="xyPoints">
        /// The xy points.
        /// </param>
        protected abstract void LeastSquaresFitLinear(IEnumerable<FitlinePoint> xyPoints, out double gain, out double offset);

        /// <summary>
        /// wrapper for updating gain and offset using LeastSquaresFitLinear
        /// </summary>
        /// <param name="xyPoints"></param>
        protected void LeastSquaresFitdLinear(IEnumerable<FitlinePoint> xyPoints)
        {
            double gain;
            double offset;

            this.LeastSquaresFitLinear(xyPoints, out gain, out offset);
            this.Slope = gain;
            this.Intercept = offset;
        }

        /// <summary>
        /// Calculate Cook's distance for all points
        /// </summary>
        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        protected void DiagnosePoints(Func<FitlinePoint, double> weightFunc)
        {
            int pointsCount = this.fitPointCollection.Count;

            if (this.state >= FitlineState.ModelComplete)
            {
                // calcaulate average X
                double meanX = 0;
                foreach (ContinuousXYPoint point in this.FitPointCollection.Select(x => x.Point))
                {
                    meanX += point.X;
                }
                meanX /= pointsCount;

                // Calculate sum of squares / SSx.
                double SSx = 0;
                foreach (ContinuousXYPoint point in this.FitPointCollection.Select(x => x.Point))
                {
                    SSx += (point.X - meanX) * (point.X - meanX);
                }

                // hat value = 1/n + (xi - x_bar)^2 / SSx
                foreach (FitlinePoint point in this.FitPointCollection)
                {
                    double leverage;
                    point.CooksD = this.CooksDistance(point.Point, SSx, meanX, pointsCount, out leverage);
                    point.Leverage = leverage;
                    point.Weight = weightFunc(point);
                }
            }
        }

        /// <summary>
        /// Compute the residual of a fit point.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        protected double ComputeResidual(ContinuousXYPoint point)
        {
            if (this.state >= FitlineState.ModelComplete)
            {
                if (this.fitPointCollection.FirstOrDefault(x => point.Equals(point)) != null)
                {
                    return point.Y - this.ModelPredictX2Y(point.X);
                }
                else
                {
                    throw new ArgumentException("Point given is not inside Fitline point list");
                }
            }
            else
            {
                throw new Exception("Has to complete model first, cannot diagnose regression");
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
        protected double CooksDistance(ContinuousXYPoint point, double ssh, double meanX, int pointsCount, out double leverage)
        {
            double distance;
            double residuel = this.ComputeResidual(point);
            double p = 3;
            double mse = this.CalculateMSE();
            distance = residuel * residuel / p / mse;
            // Get the Hii from the hat matrix
            double hii = 1.0 / pointsCount + (point.X - meanX) * (point.X - meanX) / ssh;
            distance = distance * hii / ((1 - hii) * (1 - hii));
            leverage = hii;
            return distance;
        }

        // compute the mean square error
        protected double CalculateMSE()
        {
            double sum = 0;
            foreach (ContinuousXYPoint point in this.FitPointCollection.Select(x => x.Point))
            {
                double residuel = this.ComputeResidual(point);
                sum += residuel * residuel;
            }

            return sum / this.fitPointCollection.Count;
        }

        /// <summary>
        /// Calculate R-square(Coefficient of determination)
        /// </summary>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        protected double CalculateRSquared()
        {
            if (this.state >= FitlineState.ModelComplete)
            {
                // Calculate average Y
                double avgY = 0;
                foreach (ContinuousXYPoint point in this.FitPointCollection.Select(x => x.Point))
                {
                    avgY += point.Y;
                }
                
                avgY /= this.fitPointCollection.Count;

                // Calculate explained sum of squares
                double SSreg = 0;
                double SStot = 0;
                foreach (ContinuousXYPoint point in this.FitPointCollection.Select(x => x.Point))
                {
                    double residual = this.ComputeResidual(point);
                    double residual2 = point.Y - avgY;

                    SSreg += residual * residual;
                    SStot += residual2 * residual2;
                }

                double rSquared = 1 - SSreg / SStot;

                return rSquared;
            }
            else
            {
                throw new Exception("Cannot calculate R2 without performing linear regression first");
            }
        }
    }
}  