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
            FeatureBlobStatistics statistics = featureBlob.CalculateStatistics();
            int scanImsRep = statistics.ScanImsRep;

            // Nullify the intensity score if the Scan is in unwanted areas.
            int errorMargin = (int)Math.Round(workflow.NumberOfScans * 0.01);
            if (scanImsRep < errorMargin || scanImsRep > workflow.NumberOfScans - errorMargin)
            {
                return 0;
            }

            // sum the intensities
            double summedIntensities = statistics.SumIntensities;

            // Average the intensities
            double score = summedIntensities / voltageGroup.AccumulationCount;
            
            // normalize the score
            return ScoreUtil.MapToZeroOne(score, false, globalMaxIntensity / 3);
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
        public static double PeakShapeScore(InformedWorkflow workflow, FeatureBlobStatistics statistics, VoltageGroup voltageGroup, double targetMz)
        {
            int scanRep = statistics.ScanImsRep;
            double toleranceInMZ = workflow._parameters.MassToleranceInPpm / 1e6 * targetMz;
            int scanWindowSize = workflow._parameters.ScanWindowWidth;

            if (scanRep - scanWindowSize / 2 < 0 || scanRep + scanWindowSize / 2 >= (int)workflow.NumberOfScans)
            {
                return 0;
            }

            int scanNumberMin = scanRep - scanWindowSize / 2;
            int scanNumberMax = scanRep + scanWindowSize / 2;
                                  
            int[][] intensityWindow = workflow._uimfReader.GetFramesAndScanIntensitiesForAGivenMz(
                voltageGroup.FirstFrameNumber,
                voltageGroup.FirstFrameNumber + voltageGroup.AccumulationCount,
                DataReader.FrameType.MS1, 
                scanNumberMin,
                scanNumberMax,
                targetMz,
                toleranceInMZ);

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
            double normalityScore = NormalityTest.PeakNormalityTest(averagedPeak, NormalityTest.JaqueBeraTest, 100);
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
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public static double IsotopicProfileScore(InformedWorkflow workflow, ImsTarget target, FeatureBlobStatistics statistics, List<Peak> isotopicPeakList, VoltageGroup voltageGroup, IsotopicScoreMethod selectedMethod)
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
            // Find an unsaturated peak in the isotopic profile
            for (int i = 0; i < isotopicPeakList.Count; i++)
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
                }

                sumIntensities /= voltageGroup.AccumulationCount;
                observedIsotopicPeakList.Add(sumIntensities);
            }

            // If nothing is found get out
            if (observedIsotopicPeakList.Count < 1)
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
            return dot / Math.Sqrt(theoreticalLength * observedLength);

            // double isotopicScore = Math.Acos();
            // double referenceScore = Math.Acos(1 / theoreticalLength);
            // return 1 - isotopicScore / referenceScore;
        }
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
            return Math.Sqrt(isotopicScore);
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
        /// The real peak score.
        /// </summary>
        /// <param name="workflow">
        /// The workflow.
        /// </param>
        /// <param name="bestFeature">
        /// The best feature.
        /// </param>
        /// <param name="globalMaxIntensities">
        /// The global max intensities.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double RealPeakScore(InformedWorkflow workflow, FeatureBlob bestFeature, double globalMaxIntensities)
        {
            if (bestFeature == null)
            {
                return 0;
            }

            double featureMaxIntensity = bestFeature.Statistics.IntensityMax;
            if (featureMaxIntensity * 10 >= globalMaxIntensities)
            {
                return 1;
            } 
            else if (featureMaxIntensity <= 0)
            {
                return 0;
            }
            else
            {
                return featureMaxIntensity * 10 / globalMaxIntensities;
            }
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
