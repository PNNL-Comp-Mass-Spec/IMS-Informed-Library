// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IonTransition.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the IonTransition type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation.IonSignatureMatching
{
    using System;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Scoring;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Spectrometry;

    using QuickGraph;
    using QuickGraph.Algorithms.RandomWalks;

    /// <summary>
    /// The diffusion profile comparator.
    /// </summary>
    public class IonTransition : Edge<ObservedPeak>
    {
        /// <summary>
        /// The transition probability.
        /// </summary>
        public readonly double TransitionProbability;

        /// <summary>
        /// Initializes a new instance of the <see cref="IonTransition"/> class.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="sink">
        /// The sink.
        /// </param>
        public IonTransition(ObservedPeak source, ObservedPeak sink)
            : base(source, sink)
        {
            double transitionProbability = 0;

            if (source.ObservationType == ObservationType.Peak && sink.ObservationType == ObservationType.Peak)
            {
                DiffusionProfileDescriptor descriptorSource = new DiffusionProfileDescriptor(source);
                DiffusionProfileDescriptor descriptorTarget = new DiffusionProfileDescriptor(sink);

                double mzDifference = Math.Abs(source.Peak.HighestPeakApex.MzCenterInDalton - sink.Peak.HighestPeakApex.MzCenterInDalton);

                this.MzDifferenceInPpm = UnitConversion.DaltonToPpm(
                    mzDifference,
                    (source.Peak.HighestPeakApex.MzCenterInDalton + sink.Peak.HighestPeakApex.MzCenterInDalton) / 2);

                this.IntensityDifferenceInPercentageOfMax = Math.Abs(source.Peak.SummedIntensities - sink.Peak.SummedIntensities) / Math.Max(source.Peak.SummedIntensities,     sink.Peak.SummedIntensities);

                this.DiffusionProfileDifference = new DiffusionProfileDifference(descriptorSource, descriptorTarget);

                double intensityFactor = Math.Sqrt(source.Statistics.IntensityScore * sink.Statistics.IntensityScore);
                transitionProbability = this.ComputeConsecutiveObservationMatchingProbability();
                // transitionProbability *= intensityFactor;
            }
            else if (source.ObservationType == ObservationType.Virtual)
            {
                transitionProbability = sink.NeutralLikelihoodFunction() * this.EnteringProbability(sink);
            }
            else if (sink.ObservationType == ObservationType.Virtual)
            {
                transitionProbability = this.ExitingProbability(source);
            }

            this.TransitionProbability = transitionProbability;
        }

        /// <summary>
        /// Gets the MZ difference in ppm.
        /// </summary>
        public double MzDifferenceInPpm { get; private set; }

        /// <summary>
        /// Gets the diffusion profile difference.
        /// </summary>
        public DiffusionProfileDifference DiffusionProfileDifference { get; private set; }

        /// <summary>
        /// Gets the intensity difference in percentage of max.
        /// </summary>
        public double IntensityDifferenceInPercentageOfMax { get; private set; }

        /// <summary>
        /// The cost function.
        /// </summary>
        /// <param name="transition">
        /// The transition.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private double ComputeConsecutiveObservationMatchingProbability()
        {
            // Due to draw backs of feature detector, the diffusion profile matching result is not all that reliable. So reduce weight here.
            double intensityWeight = DataAssociationParameters.IntensityWeight;
            double diffusionProfileWeight = DataAssociationParameters.DiffusionProfileWeight;
            double mzMatchWeight = DataAssociationParameters.MzMatchWeight;

            double sum = intensityWeight + diffusionProfileWeight + mzMatchWeight;
            intensityWeight /= sum;
            diffusionProfileWeight /= sum;
            mzMatchWeight /= sum;

            double intensityMatchProbability = 1 - this.IntensityDifferenceInPercentageOfMax;
            double diffusionProfileMatchProbability = this.DiffusionProfileDifference.ToDiffusionProfileMatchingProbability;
            double mzMatchProbability = ScoreUtil.MapToZeroOneExponential(this.MzDifferenceInPpm, DataAssociationParameters.MzDifferenceInPpm09, 0.9, true);

            double logResult = intensityWeight * Math.Log(intensityMatchProbability) + 
                diffusionProfileWeight * Math.Log(diffusionProfileMatchProbability) +
                mzMatchWeight * Math.Log(mzMatchProbability);

            double result = Math.Exp(logResult);
            return result;
        }

        /// <summary>
        /// The enter transition.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <returns>
        /// The <see cref="IonTransition"/>.
        /// </returns>
        private double ExitingProbability(ObservedPeak peak)
        {
            VoltageGroup vg = peak.VoltageGroup;
            return (double)vg.LastFrameNumber / vg.TotalNumberOfFramesInData;
        }

        /// <summary>
        /// The exit transition.
        /// </summary>
        /// <param name="peak">
        /// The peak.
        /// </param>
        /// <returns>
        /// The <see cref="IonTransition"/>.
        /// </returns>
        private double EnteringProbability(ObservedPeak peak)
        {
            VoltageGroup vg = peak.VoltageGroup;
            double result =  1 - (double)vg.FirstFirstFrameNumber / vg.TotalNumberOfFramesInData;
            return result;
        }
    }
}
