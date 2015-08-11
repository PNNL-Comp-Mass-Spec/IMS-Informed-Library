using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Filters
{
    using ImsInformed.Workflows.CrossSectionExtraction;

    public class ConformerFilter
    {
        public static bool FilterOnRelativeIntensity(IdentifiedIsomerInfo feature, double highestIntensities)
        {
            return true;
        }

        public static bool FilterOnPpmError(IdentifiedIsomerInfo feature, double ppmTolerance)
        {
            return (Math.Abs(feature.MzInPpm) < ppmTolerance);
        }
    }
}
