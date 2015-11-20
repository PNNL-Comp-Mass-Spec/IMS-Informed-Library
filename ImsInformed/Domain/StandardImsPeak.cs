// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
namespace ImsInformed.Domain
{
    using System;
    using System.Linq;

    using ImsInformed.Domain.DirectInjection;

    using MagnitudeConcavityPeakFinder;

    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    public struct ImsApex
    {
        public int DriftTimeCenterInScanNumber;

        public double DriftTimeCenterInMs;

        public double DriftTimeWindowToleranceInMs;

        public double DriftTimeFullWidthHalfMaxLowerBondInMs;
        
        public double DriftTimeFullWidthHalfMaxHigherBondInMs;

        public int DriftTimeFullWidthHalfMaxLowerBondInScanNumber;
        
        public int DriftTimeFullWidthHalfMaxHigherBondInScanNumber;

        public int MzCenterInBinNumber;

        public double MzCenterInDalton;

        public double MzWindowToleranceInPpm;
                      
        public double MzFullWidthHalfMaxLow;
                      
        public double MzFullWidthHalfMaxHigh;

        public int RetentionTimeCenterInMinutes;

        public int RetentionTimeCenterInFrameNumber;
    }


    /// <summary>
    /// The unified IMS peak class that provides a unified peak/feature representation for numerous different peak/feature detectors.
    /// </summary>
    [Serializable]
    public class StandardImsPeak : IEquatable<StandardImsPeak>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandardImsPeak"/> class from the feature class FeatureBlob used in Multidimensional Peak Finder.
        /// </summary>
        /// <param name="watershedFeature">
        /// The FeatureBlob feature produced from watershed.
        /// </param>
        /// <param name="uimfReader">
        /// The uimf Reader, used to provide information of the feature if the orignal feature finder does not provide.
        /// </param>
        public StandardImsPeak(FeatureBlob watershedFeature, DataReader uimfReader, VoltageGroup voltageGroup, double targetMz, double massToleranceInPpm)
        {
            // I know this is a really convoluted way for peak detection and characterization. But unless the multidimensional peak finder can perform true
            // 2D peak detections on the m/z and drift time axes instead of only rentention time and m/z time, we are stuck with this 1D peak detectors 
            // for 2D operations. Wish there is funding so we can continue developing on multidimensional peak finder.
            double tofWidthInSeconds = voltageGroup.AverageTofWidthInSeconds;

            if (watershedFeature.Statistics == null)
            {
                watershedFeature.CalculateStatistics();
            }

            this.MinDriftTimeInScanNumber = watershedFeature.Statistics.ScanImsMin;
            this.MaxDriftTimeInScanNumber = watershedFeature.Statistics.ScanImsMax;

            this.MinDriftTimeInMs = tofWidthInSeconds * this.MinDriftTimeInScanNumber * 1000;
            this.MaxDriftTimeInMs = tofWidthInSeconds * this.MaxDriftTimeInScanNumber * 1000;

            this.MinRetentionTimeInFrameNumber = watershedFeature.Statistics.ScanLcMin;
            this.MaxRetentionTimeInFrameNumber = watershedFeature.Statistics.ScanLcMax;

            ImsApex peakApex;
            int driftTimeScan = watershedFeature.Statistics.ScanImsRep;
            this.SummedIntensities = watershedFeature.Statistics.SumIntensities;
            
            peakApex.DriftTimeCenterInScanNumber = driftTimeScan;
            peakApex.DriftTimeCenterInMs = tofWidthInSeconds * driftTimeScan * 1000;

            // Note this works for direct injection. But in fact you see this method requires voltageGroup, which is only used in direct injection. You got the idea
            peakApex.RetentionTimeCenterInFrameNumber = 0;
            peakApex.RetentionTimeCenterInMinutes = 0;
            
            peakApex.MzCenterInBinNumber = 0;
            peakApex.MzCenterInDalton = 0;

            // TODO : Add centerMz edge there too.
            double[] mzArray;
            int[] intensitiesArray;
            double targetMzMin = targetMz * (1 - massToleranceInPpm / 1000000);
            double targetMzMax = targetMz * (1 + massToleranceInPpm / 1000000);

            // Somehow frame is zero indexed instead
            uimfReader.GetSpectrum(voltageGroup.FirstFrameNumber, voltageGroup.LastFrameNumber - 1, DataReader.FrameType.MS1, this.MinDriftTimeInScanNumber, this.MaxDriftTimeInScanNumber, targetMzMin, targetMzMax, out mzArray, out intensitiesArray);
            
            if (mzArray.Count() != 0)
            {
                int indexOfMax = intensitiesArray.ToList().IndexOf(intensitiesArray.Max());
                peakApex.MzCenterInDalton = mzArray[intensitiesArray.ToList().IndexOf(intensitiesArray.Max())];

                // Get the full width half max window intensity in centerMz
                double halfMax = intensitiesArray[indexOfMax] / 2;
            

                peakApex.MzFullWidthHalfMaxLow = mzArray[0];
                for (int i = indexOfMax; i > 0; i--)
                {
                    if (intensitiesArray[i] < halfMax)
                    {
                        peakApex.MzFullWidthHalfMaxLow = mzArray[i];
                    }
                }

                int count = mzArray.Count();
                peakApex.MzFullWidthHalfMaxHigh = mzArray[count - 1];
                for (int i = indexOfMax; i < mzArray.Count(); i++)
                {
                    if (intensitiesArray[i] < halfMax)
                    {
                        peakApex.MzFullWidthHalfMaxHigh = mzArray[i];
                    }
                }

                
                double deltaLowInPpm = (peakApex.MzCenterInDalton - peakApex.MzFullWidthHalfMaxLow) / peakApex.MzCenterInDalton * 1000000;
                double deltaHighInPpm = (peakApex.MzFullWidthHalfMaxHigh - peakApex.MzCenterInDalton) / peakApex.MzCenterInDalton * 1000000;
                peakApex.MzWindowToleranceInPpm = Math.Min(deltaLowInPpm, deltaHighInPpm);

                // Get the full width half max in NormalizedDriftTimeInMs
                peakApex.DriftTimeFullWidthHalfMaxHigherBondInMs = this.MaxDriftTimeInMs;
                peakApex.DriftTimeFullWidthHalfMaxLowerBondInMs = this.MinDriftTimeInMs;

                peakApex.DriftTimeFullWidthHalfMaxHigherBondInScanNumber = this.MaxDriftTimeInScanNumber;
                peakApex.DriftTimeFullWidthHalfMaxLowerBondInScanNumber = this.MinDriftTimeInScanNumber;

                peakApex.DriftTimeWindowToleranceInMs = Math.Min(Math.Abs(peakApex.DriftTimeCenterInMs - this.MinDriftTimeInMs), Math.Abs(this.MaxDriftTimeInMs   - peakApex.DriftTimeCenterInMs));

                this.PeakApex = peakApex;
                
                this.MinMzInDalton = peakApex.MzCenterInDalton - ( peakApex.MzCenterInDalton - peakApex.MzFullWidthHalfMaxLow) * 2;
                this.MaxMzInDalton = peakApex.MzCenterInDalton + (peakApex.MzFullWidthHalfMaxHigh - peakApex.MzCenterInDalton) * 2;

                // quick and dirty fix: reacquire the peak drift time apex.
                ExtractedIonChromatogram xic = ExtractedIonChromatogram.AverageXICFromFrames(uimfReader, 
                    voltageGroup.FirstFrameNumber, 
                    voltageGroup.LastFrameNumber,
                    (this.MinMzInDalton + this.MaxMzInDalton) / 2,
                    (this.MaxMzInDalton - this.MinMzInDalton) / 2 / this.PeakApex.MzCenterInDalton * 1000000,
                    (this.MinDriftTimeInMs + this.MaxDriftTimeInMs) / 2,
                    (this.MaxDriftTimeInMs - this.MinDriftTimeInMs) / 2);
                double imsScanAccurate = xic.IntensityPoints.OrderByDescending(x => x.Intensity).First().ScanIms * tofWidthInSeconds * 1000;
                peakApex.DriftTimeCenterInMs = imsScanAccurate;

                
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardImsPeak"/> class from the feature class clsPeak used in Magnitude Concavity Peak Finder.
        /// </summary>
        /// <param name="magnitudeConcavityPeakFinder">
        /// The magnitude concavity peak finder.
        /// </param>
        public StandardImsPeak(clsPeak magnitudeConcavityPeakFinder)
        {
            throw new NotImplementedException();
        }

        public double SummedIntensities { get; private set; }

        public ImsApex CenterOfMassApex { get; private set; }

        public ImsApex PeakApex { get; private set; }

        public int MinDriftTimeInScanNumber { get; private set; }

        public double MinDriftTimeInMs { get; private set; }

        public int MinMzInBinNumber { get; private set; }

        public double MinMzInDalton { get; private set; }

        public int MinRetentionTimeInMinutes { get; private set; }

        public int MinRetentionTimeInFrameNumber { get; private set; }

        public int MaxDriftTimeInScanNumber { get; private set; }

        public double MaxDriftTimeInMs { get; private set; }

        public int MaxMzInBinNumber { get; private set; }

        public double MaxMzInDalton { get; private set; }

        public int MaxRetentionTimeInMinutes { get; private set; }

        public int MaxRetentionTimeInFrameNumber { get; private set; }

        public bool Equals(StandardImsPeak other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            // If the bonding box for the feature is the same and it's from the same dataset, it is the same feautre. Caution: comparing features across
            // datasets won't work using this comparison.
            return this.MinDriftTimeInScanNumber == other.MinDriftTimeInScanNumber && this.MaxDriftTimeInScanNumber == other.MaxDriftTimeInScanNumber &&
                this.MinMzInBinNumber == other.MinMzInBinNumber && this.MaxMzInBinNumber == other.MaxMzInBinNumber &&
                this.MinRetentionTimeInFrameNumber == other.MinRetentionTimeInFrameNumber && this.MaxRetentionTimeInFrameNumber == other.MaxDriftTimeInScanNumber;
        }

        public override bool Equals(object other) 
        {
            return this.Equals(other as StandardImsPeak);
        }

        /// <summary>
        /// The get hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode() 
        {
            int result = 29;
            result = result * 13 + this.MinDriftTimeInScanNumber;
            result = result * 13 + this.MaxDriftTimeInScanNumber;
            result = result * 13 + this.MinMzInBinNumber;
            result = result * 13 + this.MaxMzInBinNumber;
            result = result * 13 + this.MinRetentionTimeInFrameNumber;
            result = result * 13 + this.MaxRetentionTimeInFrameNumber;
            return result;
        }
    }

    /// <summary>
    /// The standard ims peak util.
    /// </summary>
    public static class StandardImsPeakUtil
    {
        public static double PeakCenterLocationOnMz(this StandardImsPeak peak)
        {
            double frontalPortion = peak.PeakApex.MzCenterInDalton - peak.PeakApex.MzFullWidthHalfMaxLow;
            double fullPeakWidth = peak.PeakApex.MzFullWidthHalfMaxHigh - peak.PeakApex.MzFullWidthHalfMaxLow;
            return frontalPortion / fullPeakWidth;
        }

        public static double PeakCenterLocationOnArrivalTime(this StandardImsPeak peak)
        {
            double frontalPortion = peak.PeakApex.DriftTimeCenterInMs - peak.PeakApex.DriftTimeFullWidthHalfMaxLowerBondInMs;
            double fullPeakWidth = peak.PeakApex.DriftTimeFullWidthHalfMaxHigherBondInMs - peak.PeakApex.DriftTimeFullWidthHalfMaxLowerBondInMs;
            return frontalPortion / fullPeakWidth;
        }
    }
}