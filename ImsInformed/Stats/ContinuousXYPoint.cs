using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Stats
{
    [Serializable]
    public class ContinuousXYPoint
    {
        public double x { get; protected set; }
        public double y { get; protected set; }
        public double CooksD { get; set; }
        public bool IsOutlier { get; set; }

        public ContinuousXYPoint()
        {
            this.x = 0;
            this.y = 0;
            CooksD = 0;
            IsOutlier = false;
        }

        public ContinuousXYPoint(double x, double y)
        {
            this.x = x;
            this.y = y;
            CooksD = 0;
            IsOutlier = false;
        }
    }
}
