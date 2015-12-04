using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Statistics
{
    using DeconTools.Backend.Utilities;

    // Interatively Reweighted Least Square Fit Line
    // The difference between this and the plain least squares is points are weighted using 
    // the inverse of the cook's distance. The hope is that in the end the cook's distance are 
    // equal
    internal class IRLSFitline : FitLine
    {
        private int iterations;
        private double meanAbsoluteDeviation;
        private double tuningConstant = 4.685;

        public IRLSFitline(int iterations)
            : base()
        {
            this.iterations = iterations;
        }

        public IRLSFitline(IEnumerable<ContinuousXYPoint> fitPoints, int iterations)
            : base(fitPoints)
        {
            this.iterations = iterations;
        }

        // Calculate bisquare weight.
        // w = (abs(r) < 1) ?  (1 - r^2)^2 : 1
        private double BiSquareWeight(FitlinePoint point)
        {
            // Calculate residual
            double residual = this.ComputeResidual(point.Point);
            double adjustedResidual = residual / Math.Sqrt(1 - point.Leverage);
            double standardizedAdjustedResidual = adjustedResidual / (this.tuningConstant * this.meanAbsoluteDeviation / 0.6745);
            double biSquareWeight = Math.Pow(1 - Math.Pow(standardizedAdjustedResidual, 2), 2);
            return Math.Abs(standardizedAdjustedResidual) >= 1 ? 0 : biSquareWeight;
        }

        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        public override void PerformRegression()
        {
            this.PerformRegression(this.FitPointCollection);
        }

        // R2 is calculated differently in IRLS fit
        protected override double CalculateRSquared()
        {
            // Calculate average Y
            double avgY = 0;
            foreach (FitlinePoint point in this.FitPointCollection)
            {
                avgY += point.Point.Y * point.Weight;
            }
            
            var points = this.FitPointCollection.ToList();
            avgY /= points.Count;

            // Calculate explained sum of squares
            double SSreg = 0;
            double SStot = 0;
            foreach (FitlinePoint point in this.FitPointCollection)
            {
                double residual = this.ComputeResidual(point.Point);
                double residual2 = point.Point.Y * point.Weight - avgY;

                SSreg += Math.Pow(residual * point.Weight, 2);
                SStot += Math.Pow(residual2, 2);
            }

            double rSquared = 1 - SSreg / SStot;

            return rSquared;
        }
        
        /// <summary>
        ///     Replace the normal least square fit, using iteratively weighted least square fit.
        /// </summary>
        /// <param name="xyPoints"></param>
        /// /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        public override void PerformRegression(IEnumerable<FitlinePoint> xyPoints)
        {
            double slopeOld = 0;
            int i = 0;
            double percentDiff;

            var fitlinePoints = xyPoints as IList<FitlinePoint> ?? xyPoints.ToList();

            // Init weight
            foreach (var point in fitlinePoints)
            {
                point.Weight = 1;
            }

            // Until the gain and slope converges or max number of iterations is reached.
            do
            {
                i++;
                base.PerformRegression();
                IEnumerable<double> residuals = fitlinePoints.Select(x => this.ComputeResidual(x.Point));
                this.meanAbsoluteDeviation = this.CalculateMAD(residuals);
                this.DiagnoseRegression(this.BiSquareWeight);
                
                percentDiff = slopeOld == 0 ? 1 : Math.Abs((this.Slope - slopeOld) / slopeOld * 100);
                slopeOld = this.Slope;
            }
            while (i < this.iterations && percentDiff > 0.0000000001);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="xyPoints"></param>
        /// <param name="gain"></param>
        /// <param name="offset"></param>
        protected override void LeastSquaresFitLinear(IEnumerable<FitlinePoint> xyPoints, out double gain, out double offset)
        {
            double varianceX;
            double totalWeight = 0;
            double sumX = 0;
            double sumY = 0;
            double sumX2 = 0;
            double sumXy = 0;
            foreach (FitlinePoint point in xyPoints)
            {
                double pointWeight = point.Weight;
                totalWeight += pointWeight;
                sumX += point.Point.X * pointWeight;
                sumY += point.Point.Y * pointWeight;
                sumX2 += Math.Pow(point.Point.X, 2) * pointWeight;
                sumXy +=  point.Point.X * point.Point.Y * pointWeight;
            }

            // Calculate slope and intercept
            varianceX = totalWeight * sumX2 - Math.Pow(sumX, 2);
            offset = (sumY * sumX2 - sumX * sumXy) / varianceX;
            gain = (totalWeight * sumXy - sumX * sumY) / varianceX;
        }

        private double CalculateMAD(IEnumerable<double> inputs)
        {
            double median = this.CalculateMedian(inputs);
            IEnumerable<double> absDeviation = inputs.Select(x => Math.Abs(x - median));
            double mad = this.CalculateMedian(absDeviation);
            return mad;
        }

        private double CalculateMedian(IEnumerable<double> inputs)
        {
            var numbers = inputs.ToList();
            int halfIndex = numbers.Count()/2; 
            var sortedNumbers = numbers.OrderBy(n=>n).ToList(); 
            return sortedNumbers[halfIndex]; 
        }
    }
}
