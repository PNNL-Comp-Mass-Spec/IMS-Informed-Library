using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
	public class DriftTimeTarget
	{
		public int ChargeState { get; set; }
		public double DriftTime { get; set; }

		public DriftTimeTarget(int chargeState, double driftTime)
		{
			this.ChargeState = chargeState;
			this.DriftTime = driftTime;
		}
	}
}
