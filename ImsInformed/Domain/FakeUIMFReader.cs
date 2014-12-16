using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
    // This class store things that should have been read from UIMF file, but unfortunately not implemented
    // in the UIMF file format, this placeholder class shall be refactored out once the new UIMF lib comes out.
    public class FakeUIMFReader
    {
        public static readonly double DriftTubeLengthInCentimeters = 100;
        public static readonly double AverageScanPeriodInMicroSeconds = 163;
    }
}
