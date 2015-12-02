namespace ImsInformed.Statistics
{
    using System.Collections.Generic;
    using System.Linq;

    internal class LeastSquaresFitLine : FitLine
    {
        public LeastSquaresFitLine(IEnumerable<ContinuousXYPoint> fitPoints)
            : base(fitPoints)
        {
        }

        protected override void LeastSquaresFitLinear(IEnumerable<FitlinePoint> xyPoints, out double gain, out double offset)
        {
            int count = 0;
            double meanX = 0;
            double meanY = 0;
            double meanXSquared = 0;
            double meanXY = 0;
            foreach (ContinuousXYPoint point in xyPoints.Select(x => x.Point))
            {
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

            // Calculate slope and intercept
            gain = (meanXY - meanX * meanY) / (meanXSquared - meanX * meanX);
            offset = meanY - gain * meanX;
        }
    }
}
