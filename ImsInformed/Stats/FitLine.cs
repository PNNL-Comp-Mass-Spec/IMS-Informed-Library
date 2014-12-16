using System;
using System.Collections.Generic;

namespace ImsInformed.Stats
{
    public class FitLine
    {
        public HashSet<ContinuousXYPoint> PointCollection {get; protected set;}
        public double Intercept {get; protected set;}
        public double Slope {get; protected set;}
        public double MSE {get; protected set;}
        public double OutlierThreshold {get; protected set;}

        public FitLine(HashSet<ContinuousXYPoint> fitPoints, double outlierThreshold = 3)
        {
            this.OutlierThreshold = outlierThreshold;
            this.PointCollection = fitPoints;
            this.LeastSquaresFitLinear(fitPoints);
        }

        // Computes fit line for potential voltage group and writes
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
            this.CalculateCooksDistances();
        }

        private double ComputeResiduel(ContinuousXYPoint point)
        {
            if (this.PointCollection.Contains(point))
                return Math.Abs(this.ModelPredict(point.x) - point.y);
            else
                throw new ArgumentException("Point given is not inside Fitline point list");
        }

        // Return the predicted Y at given X
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
                if (point.CooksD >= 2)
                {
                    point.IsOutlier = true;
                }
            }
        }

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
    }
}
    