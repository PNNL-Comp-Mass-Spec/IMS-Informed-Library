using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImsInformed.Statistics
{
    // Adapter/Decorator class for continuous XY point 
    // Extra diagnostic stuff like leverage, cook's distance weight 
    // would be stored here for later manipulation
    internal class FitlinePoint : IEquatable<FitlinePoint>
    {
        public FitlinePoint(ContinuousXYPoint point)
        {
            this.Point = point;
            this.CooksD = 0;
            this.Weight = 0;
            this.Leverage = 0;
        }

        public ContinuousXYPoint Point { get; private set; }

        /// <summary>
        /// Gets or sets the cooks d.
        /// </summary>
        public double CooksD { get; set; }

        /// <summary>
        /// Gets or sets the cooks d.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Gets or sets the leverage hi
        /// </summary>
        public double Leverage { get; set; }

        public bool Equals(FitlinePoint other)
        {
            return this.Point.Equals(other.Point);
        }

        public override bool Equals(object other) 
        {
            return this.Equals(other as FitlinePoint);
        }

        public override int GetHashCode() 
        {
            return this.Point.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("X: {0:F10}, Y: {1:F2}, W: {2:F2}", this.Point.X, this.Point.Y, this.Weight);
        }
    }
}
