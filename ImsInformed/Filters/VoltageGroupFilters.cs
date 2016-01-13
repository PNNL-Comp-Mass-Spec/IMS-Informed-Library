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
namespace ImsInformed.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Domain.DirectInjection;

    /// <summary>
    /// The track filter.
    /// </summary>
    internal class VoltageGroupFilters
    {
        /// <summary>
        /// The is track possible.
        /// </summary>
        /// <param name="track">
        /// The track.
        /// </param>
        /// <param name="target"></param>
        /// <param name="crossSectionSearchParameters"></param>
        /// <param name="groups"></param>
        /// <param name="frationOfMax"></param>
        /// <returns>
        /// Voltage groups that has insufficient frames <see cref="bool"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is null.</exception>
        public static IEnumerable<VoltageGroup> RemoveVoltageGroupsWithInsufficentFrames(IEnumerable<VoltageGroup> groups, double frationOfMax)
        {
            IList<VoltageGroup> voltageGroups = groups as IList<VoltageGroup> ?? groups.ToList();
            double threshold = voltageGroups.Max(x => x.FrameAccumulationCount) * frationOfMax;
            return voltageGroups.Where(x => x.FrameAccumulationCount < threshold);
        }
    }
}

