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
using System;

namespace ImsInformed.Statistics
{
    using InformedProteomics.Backend.Data.Biology;

    /// <summary>
    /// The continuous xy point.
    /// </summary>
    [Serializable]
    internal class ContinuousXYPoint : IEquatable<ContinuousXYPoint>
    {
        /// <summary>
        /// Gets or sets the X.
        /// </summary>
        public double X { get; protected set; }

        /// <summary>
        /// Gets or sets the Y.
        /// </summary>
        public double Y { get; protected set; }

        /// <summary>
        /// Gets or sets the cooks d.
        /// </summary>
        public double CooksD { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuousXYPoint"/> class.
        /// </summary>
        public ContinuousXYPoint()
        {
            this.X = 0;
            this.Y = 0;
            this.CooksD = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuousXYPoint"/> class.
        /// </summary>
        /// <param name="x">
        /// The X.
        /// </param>
        /// <param name="y">
        /// The Y.
        /// </param>
        public ContinuousXYPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
            this.CooksD = 0;
        }

        public bool Equals(ContinuousXYPoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.X.Equals(other.X) && this.Y.Equals(other.Y);
        }

        public override bool Equals(object other) 
        {
            return this.Equals(other as ContinuousXYPoint);
        }

        public override int GetHashCode() 
        {
            int result = 29;
            result = result * 13 + this.X.GetHashCode();
            result = result * 13 + this.Y.GetHashCode();
            return result;
        }
    }
}
