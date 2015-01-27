﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureScore.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the FeatureScore type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Scoring
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using DeconTools.Backend.Core;

    using ImsInformed.Domain;
    using ImsInformed.Util;

    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    /// <summary>
    /// The feature score.
    /// </summary>
    public class FeatureScore
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
        public static double IntensityScore(InformedWorkflow workflow, FeatureBlob featureBlob, VoltageGroup voltageGroup)
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
            return ScoreUtil.MapToZeroOne(score, false, 1E6);
        }

        public static double PeakShapeScore(List<Peak> TICwindow)
        {
            return 0;
        }

        /// <summary>
        /// The score feature using isotopic profile.
        /// </summary>
        /// <param name="workflow">
        /// The workflow.
        /// </param>
        /// <param name="featureBlob">
        /// The feature blob.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="chargeState">
        /// The charge state.
        /// </param>
        /// <param name="statistics">
        /// The statistics.
        /// </param>
        /// <param name="isotopicPeakList">
        /// The isotopic peak list.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public static double IsotopicProfileScore(InformedWorkflow workflow, ImsTarget target, FeatureBlobStatistics statistics, List<Peak> isotopicPeakList, DataReader reader, VoltageGroup voltageGroup)
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
            int unsaturatedIsotope = 0;

            if (target.Composition == null)
            {
                throw new InvalidOperationException("Cannot score feature using isotopic profile for Ims target without Composition provided.");
            }

            FeatureBlob isotopeFeature = null;
            
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
                int scanWindowSize = 4;
                int scanNumberMin = (scanNumber - scanWindowSize / 2 > 0) ? scanNumber - scanWindowSize / 2 : 0;
                int scanNumberMax = (scanNumber + scanWindowSize / 2 < (int)workflow.NumberOfScans) ? scanNumber + scanWindowSize / 2 : (int)workflow.NumberOfScans - 1;
                var peakList = reader.GetXic(Mz, 
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

            // get the first isotope and use it to normalize the list
            double firstMz = observedIsotopicPeakList[0];
            for (int i = 0; i < isotopicPeakList.Count; i++)
            {
                observedIsotopicPeakList[i] /= firstMz;
            }

            // calculate the euclidean distance between theoretical distribution and observed pattern
            // double isotopicScore = 0;
            // for (int i = 1; i < isotopicPeakList.Count; i++)
            // {
            //     double diff = (observedIsotopicPeakList[i] - isotopicPeakList[i].Height);
            //     isotopicScore += diff * diff;
            // }

            // Map the score to [0, 1]
            // return ScoreUtil.MapToZeroOne(Math.Sqrt(isotopicScore), true, 0.03);

            // calculate angle between two isotopic vectors in the isotopic space
            double dot = 0;
            double theoreticalLength = 0;
            double observedLength = 0;
            for (int i = 0; i < isotopicPeakList.Count; i++)
            {
                dot += observedIsotopicPeakList[i] * isotopicPeakList[i].Height;
                theoreticalLength += isotopicPeakList[i].Height * isotopicPeakList[i].Height;
                observedLength += observedIsotopicPeakList[i] * observedIsotopicPeakList[i];
            }

            double isotopicScore = Math.Acos(dot / Math.Sqrt(theoreticalLength * observedLength));
            double referenceScore = Math.Acos(1 / theoreticalLength);
            return 1 - isotopicScore / referenceScore;
        }

        public static double RealPeakScore(InformedWorkflow workflow, FeatureBlob bestFeature, double globalMaxIntensities)
        {
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
    }
}
