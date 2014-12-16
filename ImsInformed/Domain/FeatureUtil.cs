using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
    using MultiDimensionalPeakFinding.PeakDetection;

    public class FeatureUtil
    {
        // Assess if a feature is a noise or a feature based on intensities and 
        // other metrics.
        public static bool NoiseClassier(FeatureBlob blob)
        {
            return true;
        }
    }
}
