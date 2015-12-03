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
        
        /// <summary>
        ///     Replace the normal least square fit, using iteratively weighted least square fit.
        /// </summary>
        /// <param name="xyPoints"></param>
        /// /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        public override void PerformRegression(IEnumerable<FitlinePoint> xyPoints)
        {
            var fitlinePoints = xyPoints as IList<FitlinePoint> ?? xyPoints.ToList();

            // Init weight
            foreach (var point in fitlinePoints)
            {
                point.Weight = 1;
            }

            for (int i = 0; i < this.iterations; i++)
            {
                base.PerformRegression();
                IEnumerable<double> residuals = fitlinePoints.Select(x => this.ComputeResidual(x.Point));
                this.meanAbsoluteDeviation = this.CalculateMAD(residuals);
                this.DiagnoseRegression(this.BiSquareWeight);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="xyPoints"></param>
        /// <param name="gain"></param>
        /// <param name="offset"></param>
        protected override void LeastSquaresFitLinear(IEnumerable<FitlinePoint> xyPoints, out double gain, out double offset)
        {
            int count = 0;
            double meanX = 0;
            double meanY = 0;
            double meanXSquared = 0;
            double meanXY = 0;
            foreach (FitlinePoint point in xyPoints)
            {
                count++;
                double pointWeight = point.Weight;
                double x = point.Point.X * pointWeight;
                double y = point.Point.Y * pointWeight;
                meanX += x;
                meanY += y;
                meanXSquared += Math.Pow(x, 2);
                meanXY += x * y;
            }
            meanX = meanX / count;
            meanY = meanY / count;
            meanXY = meanXY / count;
            meanXSquared = meanXSquared / count;

            // Calculate slope and intercept
            gain = (meanXY - meanX * meanY) / (meanXSquared - meanX * meanX);
            offset = meanY - gain * meanX;
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
