using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
    using UIMFLibrary;

    // This is the total ion chromatograph summed over the mz but not mobilityScan axis.
    // Which is not currently represented in the UIMF library. So I'll just implement it here.
    public class ExtractedIonChromatogram 
    {
        public double Mz {get; private set; }
        public List<IntensityPoint> IntensityPoints { get; private set; }
        public int NumberOfMobilityScans { get; private set; }

        // XIC is a list of intensity points sorted by Mobility Scan number from low to high.
        private ExtractedIonChromatogram(ExtractedIonChromatogram A, ExtractedIonChromatogram B)
        {
            if (A.NumberOfMobilityScans != B.NumberOfMobilityScans || !A.Mz.Equals(B.Mz))
                throw new InvalidOperationException("Cannot sum XICs with different mobilities or MZ.");
            this.Mz = A.Mz;
            this.NumberOfMobilityScans = B.NumberOfMobilityScans;
            this.IntensityPoints = addSortedIntensityPointList(A.IntensityPoints, B.IntensityPoints, this.NumberOfMobilityScans);
        }

        // XIC is a list of intensity points sorted by Mobility Scan number from low to high.
        public ExtractedIonChromatogram(List<IntensityPoint> XIC, DataReader uimfReader, int frameNumber, double Mz)
        {
            this.Mz = Mz;
            FrameParams param = uimfReader.GetFrameParams(frameNumber);
            NumberOfMobilityScans = param.Scans;
            IntensityPoints = XIC;
        }

        public static ExtractedIonChromatogram operator +(ExtractedIonChromatogram A, ExtractedIonChromatogram B)
        {
            ExtractedIonChromatogram result = new ExtractedIonChromatogram(A, B);
            return result;
        }

        public static List<IntensityPoint> addSortedIntensityPointList (List<IntensityPoint> A, List<IntensityPoint> B, int numberOfMobilityScans)
        {
            List<IntensityPoint> result = new List<IntensityPoint>();
            int AIndex = 0;
            int BIndex = 0;
            int frameNumber = (A.Count != 0) ? A[0].ScanLc : (B.Count != 0) ? B[0].ScanLc : 0;
            while (AIndex < A.Count || BIndex < B.Count) 
            {
                if (BIndex >= B.Count && AIndex < A.Count)
                {
                    result.Add(A[AIndex]); 
                    AIndex++;
                }
                else if (BIndex < B.Count && AIndex >= A.Count)
                {
                    result.Add(B[BIndex]); 
                    BIndex++;
                }
                else if (A[AIndex].ScanIms < B[BIndex].ScanIms)
                {
                    result.Add(A[AIndex]); 
                    AIndex++;
                } 
                else if (A[AIndex].ScanIms > B[BIndex].ScanIms)
                {
                    result.Add(B[BIndex]);
                    BIndex++;
                }
                else if (A[AIndex].ScanIms == B[BIndex].ScanIms)
                {
                    result.Add(new IntensityPoint(A[AIndex].ScanLc, A[AIndex].ScanIms, A[AIndex].Intensity + B[BIndex].Intensity));
                    AIndex++;
                    BIndex++;
                }
            }
            return result;
        }

        public static void VerifyFrameNumber(IntensityPoint point, int frameNumber)
        {
            if (point.ScanLc != frameNumber)
            {
                    throw new InvalidOperationException("Inconsistent FrameNumber for IntensityPointLists.");
            }
        }
    }
}
