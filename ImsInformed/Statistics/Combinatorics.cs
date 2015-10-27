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
namespace ImsInformed.Statistics
{
    using System;
    using System.Collections.Generic;

    internal class Combinatorics
    {
        /// <summary>
        /// The next change.
        /// </summary>
        /// <param name="binary">
        /// The binary.
        /// </param>
        /// <param name="zero2One">
        /// The zero 2 One.
        /// </param>
        /// <returns>
        /// The index for next 0 to 1 or 1 to 0 transition<see cref="int"/>.
        /// </returns>
        public static int NextChangeOnGrey(long binary, out bool zero2One)
        {
            var grey = BinaryToGray(binary);
            var greyNext = BinaryToGray(binary + 1);
            long diff = greyNext - grey;
            zero2One = diff > 0;
            diff = Math.Abs(diff);
            return (int)Math.Log(diff, 2);
        }

        /// <summary>
        /// The grey code to index of ones.
        /// </summary>
        /// <param name="greyCode">
        /// The grey code.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{T}"/>.
        /// </returns>
        public static IEnumerable<int> GreyCodeToIndexOfOnes(long greyCode)
        {
            int index = 0;
            int mask = 1;
            while (greyCode != 0)
            {
                if ((greyCode & mask) == 1)
                {
                    yield return index;
                }

                index++;
                greyCode = greyCode >> 1;
            }
        }

        /// <summary>
        /// The binary to gray.
        /// </summary>
        /// <param name="num">
        /// The num.
        /// </param>
        /// <returns>
        /// The <see cref="long"/>.
        /// </returns>
        public static long BinaryToGray(long num) 
        { 
            return (num >> 1) ^ num; 
        }
    }
}
