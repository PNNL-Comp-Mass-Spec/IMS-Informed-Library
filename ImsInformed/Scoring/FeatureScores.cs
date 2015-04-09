// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureScores.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the FeatureScores type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain;
    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Interfaces;
    using ImsInformed.Stats;
    using ImsInformed.Targets;
    using ImsInformed.Util;
    using ImsInformed.Workflows;
    using ImsInformed.Workflows.CrossSectionExtraction;
    using ImsInformed.Workflows.LcImsPeptideExtraction;

    using MathNet.Numerics.LinearAlgebra.Double;
    using MathNet.Numerics.LinearAlgebra.Generic;
    using MathNet.Numerics.Statistics;

    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    using Peak = DeconTools.Backend.Core.Peak;

    /// <summary>
    /// The feature score.
    /// </summary>
    public class FeatureScores
    {
        /// <summary>
        /// The intensity score.
        /// </summary>
        /// <param name="featurePeak">
        /// The feature blob.
        /// </param>
        /// <param name="voltageGroup">
        /// </param>
        /// <param name="globalMaxIntensity">
        /// The global Max Intensity.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityScore(StandardImsPeak featurePeak, VoltageGroup voltageGroup, double globalMaxIntensity)
        {
            // Sort features by relative intensity
            double summedIntensities = featurePeak.SummedIntensities;
            
            // Divide intensities by accumulation (If summing instead of averaging is used)
            // summedIntensities /= voltageGroup.FrameAccumulationCount;

            // normalize the score
            return ScoreUtil.MapToZeroOne(summedIntensities, false, globalMaxIntensity / 3);
        }

        /// <summary>
        /// The Peak shape score. Evaluating how "good" the peak looks. A good
        /// peak shape score indicates that the peak is not a result of noise
        /// or instrument errors. Mostly the feature intensity along is sufficient
        /// to exclude noise but a good shape score helps evaluating the experiment
        /// and thus the reliability of the data analysis result.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <param name="massToleranceInPpm">
        /// The mass Tolerance In Ppm.
        /// </param>
        /// <param name="driftTimeToleranceInMs">
        /// The drift Time Tolerance In Scans.
        /// </param>
        /// <param name="imsPeak">
        /// The imsPeak.
        /// </param>
        /// <param name="voltageGroup">
        /// The voltage group.
        /// </param>
        /// <param name="targetMz">
        /// The target MZ.
        /// </param>
        /// <param name="globalMaxIntensities">
        /// The global Max Intensities.
        /// </param>
        /// <param name="numberOfScans">
        /// The number Of Scans.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double PeakShapeScore(StandardImsPeak imsPeak, DataReader reader, double massToleranceInPpm, double driftTimeToleranceInMs, VoltageGroup voltageGroup, double globalMaxIntensities, int numberOfScans)
        {
            int scanRep = imsPeak.HighestPeakApex.DriftTimeCenterInScanNumber;
            double toleranceInMz = massToleranceInPpm / 1e6 * imsPeak.HighestPeakApex.MzCenterInDalton;
            int scanWidth = (int)Math.Ceiling(driftTimeToleranceInMs / 1000 / voltageGroup.AverageTofWidthInSeconds);
            int scanWindowSize = scanWidth * 2 + 1;

            int scanNumberMin = scanRep - scanWidth;
            int scanNumberMax = scanRep + scanWidth;
            if ((scanNumberMin < 0) || (scanNumberMax > numberOfScans - 1))
            {
                return 0;
            }
                                  
            int[][] intensityWindow = reader.GetFramesAndScanIntensitiesForAGivenMz(
                voltageGroup.FirstFrameNumber,
                voltageGroup.LastFrameNumber,
                DataReader.FrameType.MS1, 
                scanNumberMin,
                scanNumberMax,
                imsPeak.HighestPeakApex.MzCenterInDalton,
                toleranceInMz);

            // Average the intensity window across frames
            int frames = intensityWindow.GetLength(0);
            double[] averagedPeak = new double[scanWindowSize];
            double highestPeak = 0;
            for (int i = 0; i < scanWindowSize; i++)
            {
                for (int j = 0; j < frames; j++)
                {
                    averagedPeak[i] += intensityWindow[j][i];
                }

                averagedPeak[i] /= frames;
                highestPeak = (averagedPeak[i] > highestPeak) ? averagedPeak[i] : highestPeak;
            }

            // For peaks with peak width lower than 3, return a peak score of 0

            // TODO get the intensity threshold here from the noise level instead.
            if (scanWindowSize >= 3)
            {
                if (averagedPeak[scanRep - scanNumberMin] < globalMaxIntensities * 0.0001 
                    || averagedPeak[scanRep - scanNumberMin - 1] < globalMaxIntensities * 0.0001
                    || averagedPeak[scanRep - scanNumberMin + 1] < globalMaxIntensities * 0.0001)
                {
                    return 0;
                }
            }

            // Perform a statistical normality test
            double normalityScore = NormalityTest.PeakNormalityTest(averagedPeak, NormalityTest.JaqueBeraTest, 100, globalMaxIntensities);
            return normalityScore;
        }

        /// <summary>
        /// The score feature using isotopic profile.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <param name="massToleranceInPpm">
        /// The mass Tolerance In Ppm.
        /// </param>
        /// <param name="driftTimeToleranceInScans">
        /// The drift Time Tolerance In Scans.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="statistics">
        /// The imsPeak.
        /// </param>
        /// <param name="isotopicPeakList">
        /// The isotopic peak list.
        /// </param>
        /// <param name="voltageGroup">
        /// The voltage Group.
        /// </param>
        /// <param name="selectedMethod">
        /// The selected Method.
        /// </param>
        /// <param name="globalMaxIntensities">
        /// </param>
        /// <param name="numberOfScans">
        /// The number Of Scans.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public static double IsotopicProfileScore(StandardImsPeak imsPeak, DataReader reader, double massToleranceInPpm, double driftTimeToleranceInMs, IImsTarget target, List<Peak> isotopicPeakList, VoltageGroup voltageGroup, IsotopicScoreMethod selectedMethod, double globalMaxIntensities, int numberOfScans)
        {
            // No need to move on if the isotopic profile is not found
            // if (observedIsotopicProfile == null || observedIsotopicProfile.MonoIsotopicMass < 1)
            // {
            // result.AnalysisStatus = AnalysisStatus.IsotopicProfileNotFound;
            // continue;
            // }

            // Find Isotopic Profile
            // List<Peak> massSpectrumPeaks;
            // IsotopicProfile observedIsotopicProfile = _msFeatureFinder.IterativelyFindMSFeature(massSpectrum, theoreticalIsotopicProfile, out massSpectrumPeaks);
            if (target.CompositionWithoutAdduct == null)
            {
                throw new InvalidOperationException("Cannot score feature using isotopic profile for Ims target without CompositionWithoutAdduct provided.");
            }

            // Bad Feature, so get out
            if (imsPeak == null)
            {
                return 0;
            }

            // Get the scanWindow size
            int scanWindowSize = (int)Math.Ceiling(driftTimeToleranceInMs / voltageGroup.AverageTofWidthInSeconds) * 2 + 1;
            int scanRep = imsPeak.HighestPeakApex.DriftTimeCenterInScanNumber;
            int scanNumberMin = (scanRep - scanWindowSize / 2 > 0) ? scanRep - scanWindowSize / 2 : 0;
            int scanNumberMax = (scanRep + scanWindowSize / 2 < numberOfScans) ? scanRep + scanWindowSize / 2 : numberOfScans - 1;
            if ((scanNumberMin < 0) || (scanNumberMax > numberOfScans - 1))
            {
                return 0;
            }

            List<double> observedIsotopicPeakList = new List<double>();

            int totalIsotopicIndex = isotopicPeakList.Count;
            int[] isotopicIndexMask = new int[totalIsotopicIndex];

            // Find an unsaturated peak in the isotopic profile
            for (int i = 0; i < totalIsotopicIndex; i++)
            {
                // Isotopic Mz
                double Mz = isotopicPeakList[i].XValue;
                
                var peakList = reader.GetXic(Mz, 
                    massToleranceInPpm,
                    voltageGroup.FirstFrameNumber,
                    voltageGroup.LastFrameNumber,
                    scanNumberMin,
                    scanNumberMax,
                    DataReader.FrameType.MS1, 
                    DataReader.ToleranceType.PPM);

                // Sum the intensities
                double sumIntensities = 0;
                foreach (var point in peakList)
                {
                    sumIntensities += point.Intensity;
                    if (point.IsSaturated)
                    {
                        isotopicIndexMask[i] = 1;
                    }
                }

                sumIntensities /= voltageGroup.FrameAccumulationCount;
                observedIsotopicPeakList.Add(sumIntensities);
            }

            // Return 0 if the intensity sum is really small
            if (observedIsotopicPeakList.Sum() < globalMaxIntensities * 0.0003)
            {
                return 0;
            }

            // If the unsaturated isotopes are below a certain threshold
            if (totalIsotopicIndex - isotopicIndexMask.Sum() <= 1)
            {
                return 0;
            }

            if (selectedMethod == IsotopicScoreMethod.Angle)
            {
                return IsotopicProfileScoreAngle(observedIsotopicPeakList, isotopicPeakList);
            }
            else if (selectedMethod == IsotopicScoreMethod.EuclideanDistance)
            {
                return IsotopicProfileScoreEuclidean(observedIsotopicPeakList, isotopicPeakList);
            }
            else if (selectedMethod == IsotopicScoreMethod.PearsonCorrelation)
            {
                return PearsonCorrelation(observedIsotopicPeakList, isotopicPeakList);
            }
            else if (selectedMethod == IsotopicScoreMethod.Bhattacharyya)
            {
                return BhattacharyyaDistance(observedIsotopicPeakList, isotopicPeakList);
            }
            else if (selectedMethod == IsotopicScoreMethod.EuclideanDistanceAlternative)
            {
                return EuclideanAlternative(observedIsotopicPeakList, isotopicPeakList);
            }
            else 
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The score feature using isotopic profile.
        /// </summary>
        /// <param name="observedIsotopicPeakList">
        /// </param>
        /// <param name="actualIsotopicPeakList">
        /// The isotopic peak list.
        /// </param>
        /// <param name="voltageGroup">
        /// The voltage Group.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        private static double IsotopicProfileScoreEuclidean(List<double> observedIsotopicPeakList, List<Peak> actualIsotopicPeakList)
        {
            // get the first isotope and use it to normalize the list
            double firstMz = observedIsotopicPeakList[0];
            for (int i = 0; i < observedIsotopicPeakList.Count; i++)
            {
                observedIsotopicPeakList[i] /= firstMz;
            }

            // calculate the euclidean distance between theoretical distribution and observed pattern
            double isotopicScore = 0;
            for (int i = 1; i < actualIsotopicPeakList.Count; i++)
            {
                double diff = observedIsotopicPeakList[i] - actualIsotopicPeakList[i].Height;
                isotopicScore += diff * diff;
            }

            // Map the score to [0, 1]
            return ScoreUtil.MapToZeroOne(Math.Sqrt(isotopicScore), true, 0.03);
        }

        /// <summary>
        /// The isotopic profile score angle.
        /// </summary>
        /// <param name="observedIsotopicPeakList">
        /// The observed isotopic peak list.
        /// </param>
        /// <param name="actualIsotopicPeakList">
        /// The actual isotopic peak list.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private static double IsotopicProfileScoreAngle(List<double> observedIsotopicPeakList, List<Peak> actualIsotopicPeakList)
        {
            // calculate the cubic of observed isotopic peak list.
            for (int i = 0; i < observedIsotopicPeakList.Count; i++)
            {
                observedIsotopicPeakList[i] = observedIsotopicPeakList[i] * observedIsotopicPeakList[i] * observedIsotopicPeakList[i];
            }

            // calculate angle between two isotopic vectors in the isotopic space
            double dot = 0;
            double theoreticalLength = 0;
            double observedLength = 0;
            for (int i = 0; i < actualIsotopicPeakList.Count; i++)
            {
                dot += observedIsotopicPeakList[i] * actualIsotopicPeakList[i].Height;
                theoreticalLength += actualIsotopicPeakList[i].Height * actualIsotopicPeakList[i].Height;
                observedLength += observedIsotopicPeakList[i] * observedIsotopicPeakList[i];
            }

            // Return the cosine distance
            double cosine = dot / Math.Sqrt(theoreticalLength * observedLength);
            return (Math.PI / 2  - Math.Acos(cosine)) / (Math.PI / 2);
        }

        /// <summary>
        /// The pearson correlation.
        /// </summary>
        /// <param name="observedIsotopicPeakList">
        /// The observed isotopic peak list.
        /// </param>
        /// <param name="actualIsotopicPeakList">
        /// The actual isotopic peak list.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private static double PearsonCorrelation(List<double> observedIsotopicPeakList, List<Peak> actualIsotopicPeakList)
        {
            // calculate angle between two isotopic vectors in the isotopic space
            IEnumerable<double> actualIsotopicPeakListArray = actualIsotopicPeakList.Select(x => (double)x.Height);
            return Correlation.Pearson(actualIsotopicPeakListArray, observedIsotopicPeakList);
        }
        
        /// <summary>
        /// The bhattacharyya distance.
        /// </summary>
        /// <param name="observedIsotopicPeakList">
        /// The observed isotopic peak list.
        /// </param>
        /// <param name="actualIsotopicPeakList">
        /// The actual isotopic peak list.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private static double EuclideanAlternative(List<double> observedIsotopicPeakList, List<Peak> actualIsotopicPeakList)
        {
            // calculate angle between two isotopic vectors in the isotopic space
            double[] actualIsotopicPeakListArray = actualIsotopicPeakList.Select(x => (double)x.Height).ToArray();
            Vector<double> A = new DenseVector(observedIsotopicPeakList.ToArray());
            Vector<double> B = new DenseVector(actualIsotopicPeakListArray);
            A = A.Normalize(2);
            B = B.Normalize(2);

            // calculate the euclidean distance between theoretical distribution and observed pattern
            double isotopicScore = 0;
            for (int i = 1; i < actualIsotopicPeakList.Count; i++)
            {
                double diff = A[i] - B[i];
                isotopicScore += diff * diff;
            }

            // Map the score to [0, 1]
            return ScoreUtil.MapToZeroOne(Math.Sqrt(isotopicScore), true, 0.03);
        }

        /// <summary>
        /// The bhattacharyya distance.
        /// </summary>
        /// <param name="observedIsotopicPeakList">
        /// The observed isotopic peak list.
        /// </param>
        /// <param name="actualIsotopicPeakList">
        /// The actual isotopic peak list.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private static double BhattacharyyaDistance(List<double> observedIsotopicPeakList, List<Peak> actualIsotopicPeakList)
        {
            // calculate angle between two isotopic vectors in the isotopic space
            double[] actualIsotopicPeakListArray = actualIsotopicPeakList.Select(x => (double)x.Height).ToArray();
            Vector<double> A = new DenseVector(observedIsotopicPeakList.ToArray());
            Vector<double> B = new DenseVector(actualIsotopicPeakListArray);
            A = A.Normalize(2);
            B = B.Normalize(2);
            Vector<double> C = A.PointwiseMultiply(B);
            
            // Pointwise sqrt. Implements here because Math.Net.2.5 doesn't supports Pointwise exp getting Math.Net 3.5 introducces 
            // package compatibility issues with Informed Proteomics / Multidimensional peak finding, etc.
            // TODO: Use PointwiseExponent after getting Math.net 3.5
            double[] cArray = C.ToArray();
            int size = cArray.Count();
            double sum = 0;
            for (int i = 0; i < size; i++)
            {
                cArray[i] = Math.Sqrt(cArray[i]);
                sum += cArray[i];
            }

            return sum;
        }

        /// <summary>
        /// The sum feature scores.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static FeatureScoreHolder AddFeatureScores(FeatureScoreHolder a, FeatureScoreHolder b)
        {
            FeatureScoreHolder c;
            c.IntensityScore = a.IntensityScore + b.IntensityScore;
            c.IsotopicScore = a.IsotopicScore + b.IsotopicScore;
            c.PeakShapeScore = a.PeakShapeScore + b.PeakShapeScore;
            return c;
        }

        /// <summary>
        /// The average feature scores.
        /// </summary>
        /// <param name="scoreHolders">
        /// The score holders.
        /// </param>
        /// <returns>
        /// The <see cref="FeatureScoreHolder"/>.
        /// </returns>
        public static FeatureScoreHolder AverageFeatureScores(IEnumerable<FeatureScoreHolder> scoreHolders)
        {
            int count = 0;
            FeatureScoreHolder averageFeatureScores;
            averageFeatureScores.IntensityScore = 0;
            averageFeatureScores.IsotopicScore = 0;
            averageFeatureScores.PeakShapeScore = 0;

            foreach (var scoreHolder in scoreHolders)
            {
                count++;
                averageFeatureScores = AddFeatureScores(averageFeatureScores, scoreHolder);
            }

            if (count == 0)
            {
                averageFeatureScores.IntensityScore = 0;
                averageFeatureScores.IsotopicScore  = 0;
                averageFeatureScores.PeakShapeScore = 0;
                return averageFeatureScores;
            }
            else
            {
                averageFeatureScores.IntensityScore /= count;
                averageFeatureScores.IsotopicScore /= count;
                averageFeatureScores.PeakShapeScore /= count;
                return averageFeatureScores;
            }
        }
    }
}
