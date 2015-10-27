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
namespace ImsInformed.Scoring
{
    using System;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Targets;

    /// <summary>
    /// The input util.
    /// </summary>
    internal class ScoreUtil
    {
        /// <summary>
        /// The compare features function.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        public delegate double LikelihoodFunc(PeakScores featureScores);

        /// <summary>
        /// The compare feature input.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <param name="likelihoodFunc">
        /// The likelihood Func.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        private static int CompareFeatureScore(PeakScores a, PeakScores b, LikelihoodFunc likelihoodFunc)
        {
            return likelihoodFunc(a).CompareTo(likelihoodFunc(b));
        }

        /// <summary>
        /// The more likely than.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <param name="likelihoodFunc">
        /// The likelyhood func.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool MoreLikelyThanUntargeted(ObservedPeak a, ObservedPeak b, LikelihoodFunc likelihoodFunc)
        {
            int result = CompareFeatureScore(a.Statistics, b.Statistics, likelihoodFunc);
            return result > 0;
        }

        /// <summary>
        /// The more likely than targeted.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <param name="likelihoodFunc">
        /// The likelihood func.
        /// </param>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public static bool MoreLikelyThanTargeted(ObservedPeak a, ObservedPeak b, LikelihoodFunc likelihoodFunc, IImsTarget target)
        {
            throw new NotImplementedException();
        }

        // e^-ax
        public static double MapToZeroOneExponential(double input, double a, bool inverseMapping = false)
        {
            double result = 1 - Math.Exp(0 - a * input);
            return inverseMapping ? 1- result : result;
        }

        /// <summary>
        /// The map to zero one using 1- e^-ax
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <param name="x0">
        /// The x 0.
        /// </param>
        /// <param name="y0">
        /// The y 0.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static double MapToZeroOneExponential(double input, double x0, double y0, bool inverseMapping = false)
        {
            if (y0 >= 1 || y0 <= 0 || input < 0)
            {
                throw new ArgumentException();
            }

            double a = 0 - Math.Log(y0) / x0;
            double result = 1 - Math.Exp(0 - a * input);
            return inverseMapping ? 1 - result : result;
        }

        /// <summary>
        /// Map a double from 0 to inifinity to [0,1] Range
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <param name="inverseMapping">
        /// The inverse mapping.
        /// </param>
        /// <param name="ninetyPercentX">
        /// The X value to map to 0.9. Used to define a "good" input.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double MapToZeroOneTrignometry(double input, bool inverseMapping, double ninetyPercentX)
        {
            double scale = 0;
            
            // Compute the scale from ninetyPercentX

            if (inverseMapping)
            {
                // Solve (Pi/2 - atan(X)) / (PI/2) = 0.9
                scale = Math.Tan(0.1 * Math.PI / 2) / ninetyPercentX;
            }
            else
            {
                // Solve atan(X) / (PI/2) = 0.9
                scale = Math.Tan(0.9 * Math.PI / 2) / ninetyPercentX;
            }

            double normalizedScore = input * scale;
            if (inverseMapping)
            {
                normalizedScore = Math.PI / 2 - Math.Atan(normalizedScore);
            }
            else
            {
                normalizedScore = Math.Atan(normalizedScore);
            }

            normalizedScore /= Math.PI / 2;
            return normalizedScore;
        }
    }
}
