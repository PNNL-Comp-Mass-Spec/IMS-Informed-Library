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

    using ImsInformed.Scoring;

    /// <summary>
    ///     The diffusion profile difference.
    /// </summary>
    internal class DiffusionProfileDifference
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DiffusionProfileDifference"/> class.
        /// </summary>
        /// <param name="thisDescriptor">
        /// The this descriptor.
        /// </param>
        /// <param name="otherDescriptor">
        /// The other descriptor.
        /// </param>
        public DiffusionProfileDifference(
            DiffusionProfileDescriptor thisDescriptor, 
            DiffusionProfileDescriptor otherDescriptor)
        {
            this.ArrivalTimeDiffusionWidthDifferenceInMs =
                Math.Abs(thisDescriptor.ArrivalTimeDiffusionWidthInMs - otherDescriptor.ArrivalTimeDiffusionWidthInMs);
            this.MzDiffusionWidthDifferenceInPpm =
                Math.Abs(thisDescriptor.MzDiffusionWidthInPpm - otherDescriptor.MzDiffusionWidthInPpm);
            this.MzCenterLocationDifference =
                Math.Abs(thisDescriptor.MzCenterLocation - otherDescriptor.MzCenterLocation);
            this.ArrivalTimeCenterLocationDifference =
                Math.Abs(thisDescriptor.ArrivalTimeCenterLocation - otherDescriptor.ArrivalTimeCenterLocation);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the arrival time center location.
        /// </summary>
        public double ArrivalTimeCenterLocationDifference { get; private set; }

        /// <summary>
        ///     Gets the arrival time diffusion width.
        /// </summary>
        public double ArrivalTimeDiffusionWidthDifferenceInMs { get; private set; }

        /// <summary>
        ///     Gets the MZ center location.
        /// </summary>
        public double MzCenterLocationDifference { get; private set; }

        /// <summary>
        ///     Gets the MZ diffusion width.
        /// </summary>
        public double MzDiffusionWidthDifferenceInPpm { get; private set; }

        /// <summary>
        /// The to diffusion profile matching probability.
        /// </summary>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double ToDiffusionProfileMatchingProbability
        {
            get
            {
                double probability = 1;

                // probability *= ScoreUtil.MapToZeroOneExponential(this.ArrivalTimeCenterLocationDifference, 0.2, 0.9, true);
                // probability *= ScoreUtil.MapToZeroOneExponential(this.ArrivalTimeDiffusionWidthDifferenceInMs, 0.2, 0.9, true);
                probability *= ScoreUtil.MapToZeroOneExponential(this.MzCenterLocationDifference, 0.1, 0.9, true);
                probability *= ScoreUtil.MapToZeroOneExponential(this.MzDiffusionWidthDifferenceInPpm, 20, 0.9, true);

                return probability;
            }
        }

        #endregion
    }
}