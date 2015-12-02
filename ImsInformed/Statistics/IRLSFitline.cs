namespace ImsInformed.Statistics
{
    using System;
    using System.Collections.Generic;

    // Interatively Reweighted Least Square Fit Line
    // The difference between this and the plain least squares is points are weighted using 
    // the inverse of the cook's distance. The hope is that in the end the cook's distance are 
    // equal
    internal class IRLSFitline : FitLine
    {
        private int iterations;
        private double meanAbsoluteDeviation;
        private double tuningConstant = 4.685;

        public IRLSFitline(IEnumerable<ContinuousXYPoint> fitPoints, int iterations)
            : base(fitPoints)
        {
            this.iterations = iterations;
        }

        /// <exception cref="ArgumentNullException"><paramref name="other" /> is null.</exception>
        protected void LeastSquaresFitLinear(IEnumerable<ContinuousXYPoint> xyPoints)
        {
            
        }

        // Calculate bisquare weight.
        private double BiSquareWeight(FitlinePoint point)
        {
            // Calculate residual
            double residual = this.ComputeResidual(point.Point);
            double adjustedResidual = residual / Math.Sqrt(1 - point.Leverage);
            double standardizedAdjustedResidual = adjustedResidual / (this.tuningConstant * this.meanAbsoluteDeviation);
            double biSquareWeight = Math.Pow(1 - Math.Pow(standardizedAdjustedResidual, 2), 2);
            return Math.Abs(standardizedAdjustedResidual) >= 1 ? 0 : biSquareWeight;
        }

        protected override void LeastSquaresFitLinear(IEnumerable<FitlinePoint> xyPoints, out double gain, out double offset)
        {

            // Iterative method
            for (int i = 0; i < this.iterations; i++)
            {
                //this.BiSquareWeightedLeastSquares(this.fitPointCollection, this.BiSquareWeight);
            }

            gain = 1;
            offset = 1;
        }
    }
}
