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
    using System.Windows.Navigation;

    using InformedProteomics.Backend.Data.Spectrometry;

    using MathNet.Numerics.Integration.Algorithms;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Double;
    using MathNet.Numerics.LinearAlgebra.Generic;
    using MathNet.Numerics.Statistics;

    using ImsInformed.Domain;
    using ImsInformed.Stats;
    using ImsInformed.Util;

    using MultiDimensionalPeakFinding.PeakDetection;

    using PNNLOmics.Algorithms.Distance;

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
        /// <param name="workflow">
        /// The workflow.
        /// </param>
        /// <param name="featureBlob">
        /// The feature blob.
        /// </param>
        /// <param name="voltageGroup"></param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double IntensityScore(InformedWorkflow workflow, FeatureBlob featureBlob, VoltageGroup voltageGroup, double globalMaxIntensity)
        {
            // Sort features by relative intensity
            FeatureBlobStatistics statistics = featureBlob.Statistics;
            double summedIntensities = statistics.SumIntensities;
            
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
        /// <param name="workflow">
        /// The workflow.
        /// </param>
        /// <param name="statistics">
        /// The statistics.
        /// </param>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <param name="voltageGroup">
        /// The voltage group.
        /// </param>
        /// <param name="targetMz">
        /// The target mz.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double PeakShapeScore(InformedWorkflow workflow, FeatureBlobStatistics statistics, VoltageGroup voltageGroup, double targetMz, double globalMaxIntensities)
        {
            int scanRep = statistics.ScanImsRep;
            double toleranceInMz = workflow._parameters.MassToleranceInPpm / 1e6 * targetMz;
            int scanWindowSize = workflow._parameters.ScanWindowWidth;

            int scanNumberMin = scanRep - scanWindowSize / 2;
            int scanNumberMax = scanRep + scanWindowSize / 2;
                                  
            int[][] intensityWindow = workflow._uimfReader.GetFramesAndScanIntensitiesForAGivenMz(
                voltageGroup.FirstFrameNumber,
                voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount,
                DataReader.FrameType.MS1, 
                scanNumberMin,
                scanNumberMax,
                targetMz,
                toleranceInMz);

            // Average the intensity window across frames
            int frames = intensityWindow.GetLength(0);
            int scans = scanWindowSize / 2 * 2 + 1;
            double[] averagedPeak = new double[scans];
            double highestPeak = 0;
            for (int i = 0; i < scans; i++)
            {
                for (int j = 0; j < frames; j++)
                {
                    averagedPeak[i] += intensityWindow[j][i];
                }

                averagedPeak[i] /= frames;
                highestPeak = (averagedPeak[i] > highestPeak) ? averagedPeak[i] : highestPeak;
            }

            // Perform a statistical normality test
            double normalityScore = NormalityTest.PeakNormalityTest(averagedPeak, NormalityTest.JaqueBeraTest, 100, globalMaxIntensities);
            return normalityScore;
        }

        /// <summary>
        /// The score feature using isotopic profile.
        /// </summary>
        /// <param name="workflow">
        /// The workflow.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="statistics">
        /// The statistics.
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
        /// <param name="globalMaxIntensities"></param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public static double IsotopicProfileScore(InformedWorkflow workflow, ImsTarget target, FeatureBlobStatistics statistics, List<Peak> isotopicPeakList, VoltageGroup voltageGroup, IsotopicScoreMethod selectedMethod, double globalMaxIntensities)
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
            if (target.Composition == null)
            {
                throw new InvalidOperationException("Cannot score feature using isotopic profile for Ims target without Composition provided.");
            }

            // Bad Feature, so get out
            if (statistics == null)
            {
                return 0;
            }
            
            int scanNumber = statistics.ScanImsRep;
            List<double> observedIsotopicPeakList = new List<double>();
            
            // Return 0 if the intensity sum is really small
            if (observedIsotopicPeakList.Sum() < globalMaxIntensities * 0.0001)
            {
                return 0;
            }

            int totalIsotopicIndex = isotopicPeakList.Count;
            int[] isotopicIndexMask = new int[totalIsotopicIndex];

            // Find an unsaturated peak in the isotopic profile
            for (int i = 0; i < totalIsotopicIndex; i++)
            {
                // Isotopic Mz
                double Mz = isotopicPeakList[i].XValue;
                int scanWindowSize = workflow._parameters.ScanWindowWidth;
                int scanNumberMin = (scanNumber - scanWindowSize / 2 > 0) ? scanNumber - scanWindowSize / 2 : 0;
                int scanNumberMax = (scanNumber + scanWindowSize / 2 < (int)workflow.NumberOfScans) ? scanNumber + scanWindowSize / 2 : (int)workflow.NumberOfScans - 1;
                var peakList = workflow._uimfReader.GetXic(Mz, 
                    workflow._parameters.MassToleranceInPpm / 10,
                    voltageGroup.FirstFrameNumber,
                    voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount - 1,
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

                sumIntensities /= voltageGroup.AccumulationCount;
                observedIsotopicPeakList.Add(sumIntensities);
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

            averageFeatureScores.IntensityScore /= count;
            averageFeatureScores.IsotopicScore /= count;
            averageFeatureScores.PeakShapeScore /= count;
            return averageFeatureScores;
        }
    }
}
