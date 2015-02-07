namespace ImsInformed.Domain
{
    using System;
    using System.Collections.Generic;

    using UIMFLibrary;

    /// <summary>
    /// This is the XIC summed over the mz but not mobilityScan axis.
    /// </summary>
    public class ExtractedIonChromatogram 
    {
        /// <summary>
        /// Gets the MZ.
        /// </summary>
        public double Mz {get; private set; }

        /// <summary>
        /// Gets the intensity points.
        /// </summary>
        public List<IntensityPoint> IntensityPoints { get; private set; }

        /// <summary>
        /// Gets the number of mobility scans.
        /// </summary>
        public int NumberOfMobilityScans { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractedIonChromatogram"/> class. 
        /// XIC is a list of intensity points sorted by Mobility Scan number from low to high.
        /// </summary>
        /// <param name="A">
        /// The a.
        /// </param>
        /// <param name="B">
        /// The b.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        private ExtractedIonChromatogram(ExtractedIonChromatogram A, ExtractedIonChromatogram B)
        {
            if (A.NumberOfMobilityScans != B.NumberOfMobilityScans || !A.Mz.Equals(B.Mz))
                throw new InvalidOperationException("Cannot sum XICs with different mobilities or MZ.");
            this.Mz = A.Mz;
            this.NumberOfMobilityScans = B.NumberOfMobilityScans;
            this.IntensityPoints = addSortedIntensityPointList(A.IntensityPoints, B.IntensityPoints, this.NumberOfMobilityScans);
        }

        // XIC is a dictionary represented TIC sorted by Mobility Scan number from low to high.
        public ExtractedIonChromatogram(List<IntensityPoint> XIC, DataReader uimfReader, int frameNumber, double Mz)
        {
            this.Mz = Mz;
            FrameParams param = uimfReader.GetFrameParams(frameNumber);
            NumberOfMobilityScans = param.Scans;
            IntensityPoints = XIC;
        }

        /// <summary>
        /// The +.
        /// </summary>
        /// <param name="A">
        /// The a.
        /// </param>
        /// <param name="B">
        /// The b.
        /// </param>
        /// <returns>
        /// </returns>
        public static ExtractedIonChromatogram operator +(ExtractedIonChromatogram A, ExtractedIonChromatogram B)
        {
            ExtractedIonChromatogram result = new ExtractedIonChromatogram(A, B);
            return result;
        }

        /// <summary>
        /// Add sorted intensity point list. Classical algorithm for merging 2 sorted list.
        /// </summary>
        /// <param name="A">
        /// The a.
        /// </param>
        /// <param name="B">
        /// The b.
        /// </param>
        /// <param name="numberOfMobilityScans">
        /// The number of mobility scans.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
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

        /// <summary>
        /// The verify frame number.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="frameNumber">
        /// The frame number.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public static void VerifyFrameNumber(IntensityPoint point, int frameNumber)
        {
            if (point.ScanLc != frameNumber)
            {
                    throw new InvalidOperationException("Inconsistent FrameNumber for IntensityPointLists.");
            }
        }
    }
}
