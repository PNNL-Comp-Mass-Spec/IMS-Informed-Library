// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StandardImsPeak.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The unified IMS peak class that provides a unified peak/feature representation for numerous different peak/feature detectors.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

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

            ImsApex highestPeakApex;
            int driftTimeScan = watershedFeature.Statistics.ScanImsRep;
            this.SummedIntensities = watershedFeature.Statistics.SumIntensities;
            
            highestPeakApex.DriftTimeCenterInScanNumber = driftTimeScan;
            highestPeakApex.DriftTimeCenterInMs = tofWidthInSeconds * driftTimeScan * 1000;

            // Note this works for direct injection. But in fact you see this method requires voltageGroup, which is only used in direct injection. You got the idea
            highestPeakApex.RetentionTimeCenterInFrameNumber = 0;
            highestPeakApex.RetentionTimeCenterInMinutes = 0;
            
            highestPeakApex.MzCenterInBinNumber = 0;
            highestPeakApex.MzCenterInDalton = 0;

            // TODO : Add Mz edge there too.
            double[] mzArray;
            int[] intensitiesArray;
            double targetMzMin = targetMz * (1 - massToleranceInPpm / 1000000);
            double targetMzMax = targetMz * (1 + massToleranceInPpm / 1000000);

            // Somehow frame is zero indexed instead
            uimfReader.GetSpectrum(voltageGroup.FirstFrameNumber, voltageGroup.LastFrameNumber - 1, DataReader.FrameType.MS1, this.MinDriftTimeInScanNumber, this.MaxDriftTimeInScanNumber, targetMzMin, targetMzMax, out mzArray, out intensitiesArray);
            
            int indexOfMax = intensitiesArray.ToList().IndexOf(intensitiesArray.Max());
            highestPeakApex.MzCenterInDalton = mzArray[intensitiesArray.ToList().IndexOf(intensitiesArray.Max())];

            // Get the full width half max window in Mz
            double halfMax = highestPeakApex.MzCenterInDalton / 2;

            highestPeakApex.MzFullWidthHalfMaxLow = mzArray[0];
            for (int i = indexOfMax; i > 0; i--)
            {
                if (intensitiesArray[i] < halfMax)
                {
                    highestPeakApex.MzFullWidthHalfMaxLow = mzArray[i];
                }
            }

            int count = mzArray.Count();
            highestPeakApex.MzFullWidthHalfMaxHigh = mzArray[count - 1];
            for (int i = indexOfMax; i < mzArray.Count(); i++)
            {
                if (intensitiesArray[i] < halfMax)
                {
                    highestPeakApex.MzFullWidthHalfMaxHigh = mzArray[i];
                }
            }

            double deltaLowInPpm = (highestPeakApex.MzCenterInDalton - highestPeakApex.MzFullWidthHalfMaxLow) / highestPeakApex.MzCenterInDalton * 1000000;
            double deltaHighInPpm = (highestPeakApex.MzFullWidthHalfMaxHigh - highestPeakApex.MzCenterInDalton) / highestPeakApex.MzCenterInDalton * 1000000;
            highestPeakApex.MzWindowToleranceInPpm = Math.Min(deltaLowInPpm, deltaHighInPpm);

            // Get the full width half max in DriftTime
            highestPeakApex.DriftTimeFullWidthHalfMaxHigherBondInMs = this.MaxDriftTimeInMs;
            highestPeakApex.DriftTimeFullWidthHalfMaxLowerBondInMs = this.MinDriftTimeInMs;

            highestPeakApex.DriftTimeFullWidthHalfMaxHigherBondInScanNumber = this.MaxDriftTimeInScanNumber;
            highestPeakApex.DriftTimeFullWidthHalfMaxLowerBondInScanNumber = this.MaxDriftTimeInScanNumber;

            highestPeakApex.DriftTimeWindowToleranceInMs = Math.Min(Math.Abs(highestPeakApex.DriftTimeCenterInMs - this.MinDriftTimeInMs), Math.Abs(this.MaxDriftTimeInMs - highestPeakApex.DriftTimeCenterInMs));

            this.HighestPeakApex = highestPeakApex;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardImsPeak"/> class from the feature class clsPeak used in Magnitude Concavity Peak Finder.
        /// </summary>
        /// <param name="magnitudeConcavityPeakFinder">
        /// The magnitude concavity peak finder.
        /// </param>
        public StandardImsPeak(clsPeak magnitudeConcavityPeakFinder)
        {
            
        }

        public double SummedIntensities { get; private set; }

        public ImsApex CenterOfMassApex { get; private set; }

        public ImsApex HighestPeakApex { get; private set; }

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
}
