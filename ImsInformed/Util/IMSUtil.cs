using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Util
{
    using UIMFLibrary;

    public class IMSUtil
    {
        /// <summary>
        /// The pad zeroes to point list.
        /// </summary>
        /// <param name="sparsePointList">
        /// The sparse point list. Note that this method is written for sorrted 1D data where LcScan is invariant.
        /// </param>
        /// <param name="destinationSize">
        /// The destination size.
        /// </param>
        /// <returns>
        /// The <see cref="IList"/>.
        /// </returns>
        public static IList<IntensityPoint> PadZeroesToPointList(IList<IntensityPoint> sparsePointList, int destinationSize)
        {
            if (destinationSize < sparsePointList.Last().ScanIms)
            {
                throw new InvalidOperationException("destinationSize has to be greater then original size");
            }

            IList<IntensityPoint> newList = new List<IntensityPoint>();
            int lc = sparsePointList.First().ScanLc;

            int oldListIndex = 0;
            for (int scanIms = 1; scanIms <= destinationSize; scanIms++)
            {
                if (oldListIndex >= sparsePointList.Count || scanIms < sparsePointList[oldListIndex].ScanIms)
                {
                    newList.Add(new IntensityPoint(lc, scanIms, 0));
                } 
                else if (scanIms == sparsePointList[oldListIndex].ScanIms)
                {
                    newList.Add(sparsePointList[oldListIndex]);
                    oldListIndex++;
                }
                else
                {
                    throw new InvalidOperationException("Logical error");
                }
            }
            return newList;
        }
    }
}
