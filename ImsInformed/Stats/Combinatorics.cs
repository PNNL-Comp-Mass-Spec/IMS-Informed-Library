// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Combinatorics.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the Combinatorics type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Stats
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;

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
        /// The <see cref="IEnumerable"/>.
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
