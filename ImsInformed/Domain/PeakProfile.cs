using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Domain
{
    using MultiDimensionalPeakFinding.PeakDetection;

    // Standard representation of the IMS peak profile
    public class PeakProfile
    {
        // A list of peaks represented by an array of points, whereas the index is the isotopic index
        public IList<IEnumerable<Point>>  Data { get; private set;}
    }
}
