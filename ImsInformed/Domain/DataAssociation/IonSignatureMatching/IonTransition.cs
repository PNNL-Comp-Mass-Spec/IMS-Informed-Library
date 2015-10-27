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
namespace ImsInformed.Domain.DataAssociation.IonSignatureMatching
{
    using System;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Scoring;
    using ImsInformed.Util;

    using QuickGraph;

    /// <summary>
    /// The diffusion profile comparator.
    /// </summary>
    internal class IonTransition : Edge<ObservedPeak>
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

                this.MzDifferenceInPpm = Metrics.DaltonToPpm(
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
            double intensityWeight = DataAssociationTuningParameters.IntensityWeight;
            double diffusionProfileWeight = DataAssociationTuningParameters.DiffusionProfileWeight;
            double mzMatchWeight = DataAssociationTuningParameters.MzMatchWeight;

            double sum = intensityWeight + diffusionProfileWeight + mzMatchWeight;
            intensityWeight /= sum;
            diffusionProfileWeight /= sum;
            mzMatchWeight /= sum;

            double intensityMatchProbability = 1 - this.IntensityDifferenceInPercentageOfMax;
            double diffusionProfileMatchProbability = this.DiffusionProfileDifference.ToDiffusionProfileMatchingProbability;
            double mzMatchProbability = ScoreUtil.MapToZeroOneExponential(this.MzDifferenceInPpm, DataAssociationTuningParameters.MzDifferenceInPpm09, 0.9, true);

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
