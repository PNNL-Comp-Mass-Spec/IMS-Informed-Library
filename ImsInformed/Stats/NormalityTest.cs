﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NormalityTest.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The normality test.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Stats
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DeconTools.Backend.Core;

    using ImsInformed.Scoring;

    using InformedProteomics.Backend.Data.Spectrometry;

    using MathNet.Numerics.Statistics;

    /// <summary>
    /// The normality test.
    /// </summary>
    public class NormalityTest
    {
        /// <summary>
        /// The normality test.
        /// </summary>
        /// <param name="samples">
        /// The samples.
        /// </param>
        public delegate double NormalityTestFunc(double[] samples);

        /// <summary>
        /// The peak normality test.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <param name="normalityTestFunc">
        /// The normality test func.
        /// </param>
        /// <param name="numberOfSamples">
        /// The number of samples.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double PeakNormalityTest(double[] peak, NormalityTestFunc normalityTestFunc, int numberOfSamples)
        {
            double[] peakRandomVar = PeakToRandomVariable(peak, numberOfSamples);
            if (peakRandomVar == null)
            {
                return 0;
            }

            return normalityTestFunc(peakRandomVar);
        }

        /// <summary>
        /// The kolmogorov smirnov test.
        /// </summary>
        /// <param name="sampleList">
        /// The sample list.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double AndersonDarlingTest(double[] sampleList)
        {
            //NormalDistribution distrubution = NormalDistribution.Standard;
            //return 0;
            throw new NotImplementedException();
        }

        public static double KolmogorovSmirnovTest(double[] sampleList)
        {
            // AndersonDarlingTest adTest = new AndersonDarlingTest(sampleList, new NormalDistribution());
            // var distri = adTest.TheoreticalDistribution;
            // var sig = adTest.Significant;
            // var p = adTest.PValue;
            // return 0;
            throw new NotImplementedException();
        }

        public static double JaqueBeraTest(double[] sampleList)
        {
            // Get the Skewness and Kurtosis in a single run using Math.Net
            DescriptiveStatistics statistics = new DescriptiveStatistics(sampleList);
            double kurtosis = statistics.Kurtosis;
            double skewness = statistics.Skewness;
            int sampleCount = sampleList.Length;
            double JB = sampleCount / 6 * (Math.Pow(skewness, 2) + Math.Pow(kurtosis - 3, 2) / 4);

            // Because of really small sample, use a table of distrubution quantitiles generated by Monte Carlo simulation
            // P-value           JB
            // 0.10              4.61
            // 0.05              5.99
            // 0.01              9.21 
            // Lower p value reject the normal distrubution hypothesis. Higher JB means lower p-value. So higher JB rejects
            // H0, So lower JB means higher score. 
            return ScoreUtil.MapToZeroOne(JB, true, 9.21);
        }

        /// <summary>
        /// Theoretically the larger the sample size, the better. Note that the peak descriptor will be modified here.
        /// </summary>
        /// <param name="peakDescriptor">
        /// The peak descriptor.
        /// </param>
        /// <param name="numberOfSamples">
        /// The number of samples.
        /// </param>
        /// <returns>
        /// The <see cref="int[]"/>.
        /// </returns>
        public static double[] PeakToRandomVariable(double[] peakDescriptor, int numberOfSamples)
        {
            double sum = peakDescriptor.Sum();

            if (sum == 0)
            {
                return null;
            }

            // Normallize the peak to a frequency diagram with the sum frequency of numberOfSamples
            for (int i = 0; i < peakDescriptor.Length; i++)
            {
                double frequency  = (int)Math.Ceiling(peakDescriptor[i] * numberOfSamples / sum);
                peakDescriptor[i] = frequency; 
            }

            // sort the frequency diagram from high to low while maintaining index.
            var sorted = peakDescriptor
            .Select((x, i) => new KeyValuePair<double, int>(x, i))
            .OrderByDescending(x => x.Key)
            .ToArray();

            double[] sortedPeakDescriptor = sorted.Select(x => x.Key).ToArray();
            int[] idx = sorted.Select(x => x.Value).ToArray();

            // Convert the sorted peak array into cumulative one
            double cuminlative = 0;
            for (int i = 0; i < sortedPeakDescriptor.Length; i++)
            {
                cuminlative += sortedPeakDescriptor[i];
                sortedPeakDescriptor[i] = cuminlative; 
            }

            double[] result = new double[numberOfSamples];
            for (int i = 0; i < numberOfSamples; i++)
            {
                // Find the index in peakDescriptor where i is immediately greater than
                int j;
                for (j = 0; j < sortedPeakDescriptor.Length; j++)
                {
                    if (i < sortedPeakDescriptor[j])
                    {
                        break;
                    }
                }

                result[i] = idx[j];
            }

            return result;
        }
    }
}
