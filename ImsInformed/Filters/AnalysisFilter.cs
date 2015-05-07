using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Filters
{
    using MultiDimensionalPeakFinding.PeakDetection;

    public class AnalysisFilter
    {
        public static bool FilterLowR2(double r2, double r2Threshold = 0.9)
        {
            return r2 < r2Threshold;
        }

        public static bool FilterLowFitPointNumber(int points, int minPoints)
        {
            return points < minPoints;
        }
    }
}
