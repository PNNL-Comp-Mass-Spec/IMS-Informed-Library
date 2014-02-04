using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend;
using DeconTools.Backend.Core;

namespace ImsInformed.Util
{
	public class IsotopicProfileUtil
	{
		public static void AdjustSaturatedIsotopicProfile(double[] profileToAdjust, float[] theoreticalProfile, int indexOfIsotopeToUse)
		{
			//ensure targetPeak is within range
			if (indexOfIsotopeToUse >= theoreticalProfile.Length)
			{
				return;
			}

			double intensityObsPeakForExtrapolation = profileToAdjust[indexOfIsotopeToUse];
			float intensityTheorPeakForExtrapolation = theoreticalProfile[indexOfIsotopeToUse];

			for (int i = 0; i < profileToAdjust.Length; i++)
			{
				if (i < indexOfIsotopeToUse)
				{
					profileToAdjust[i] = theoreticalProfile[i] * intensityObsPeakForExtrapolation / intensityTheorPeakForExtrapolation;
				}
				else
				{
					break;
				}
			}
		}

		public static void AdjustSaturatedIsotopicProfile(IsotopicProfile iso, IsotopicProfile theorIsotopicProfile, int indexOfIsotopeToUse, bool updatePeakMasses = true, bool updatePeakIntensities = true)
		{
			//ensure targetPeak is within range
			if (indexOfIsotopeToUse >= theorIsotopicProfile.Peaklist.Count)
			{
				return;
			}

			MSPeak observedPeakOfIsotopeToUse = iso.Peaklist[indexOfIsotopeToUse];
			float intensityObsPeakForExtrapolation = observedPeakOfIsotopeToUse.Height;
			float widthObsPeakForExtrapolation = observedPeakOfIsotopeToUse.Width;
			var xValueObsPeakForExteapolation = observedPeakOfIsotopeToUse.XValue;
			float intensityTheorPeakForExtrapolation = theorIsotopicProfile.Peaklist[indexOfIsotopeToUse].Height;

			for (int i = 0; i < iso.Peaklist.Count; i++)
			{
				if (i < indexOfIsotopeToUse)
				{
					MSPeak currentPeak = iso.Peaklist[i];

					if (updatePeakIntensities)
					{
						if (i >= theorIsotopicProfile.Peaklist.Count)
						{
							currentPeak.Height = 0;
						}
						else
						{
							currentPeak.Height = theorIsotopicProfile.Peaklist[i].Height * intensityObsPeakForExtrapolation / intensityTheorPeakForExtrapolation;
						}

						currentPeak.Width = widthObsPeakForExtrapolation;    //repair the width too, because it can get huge. Width can be used in determining tolerances.
					}

					//correct the m/z value, to more accurately base it on the non-saturated peak.  See Chernushevich et al. 2001 http://onlinelibrary.wiley.com/doi/10.1002/jms.207/abstract
					if (updatePeakMasses)
					{
						// formula is  MZ0 = MZ3 - (1.003/z)*n, where MZ0 is the m/z of the saturated peak and MZ3 the m/z of the nonSaturated peak and z is charge state and n is the difference in peak number
						currentPeak.XValue = xValueObsPeakForExteapolation - ((Globals.MASS_DIFF_BETWEEN_ISOTOPICPEAKS) / iso.ChargeState * (indexOfIsotopeToUse - i));
					}

				}
			}

			iso.IntensityMostAbundant = iso.getMostIntensePeak().Height;

			int indexMostAbundantPeakTheor = theorIsotopicProfile.GetIndexOfMostIntensePeak();

			if (iso.Peaklist.Count > indexMostAbundantPeakTheor)
			{
				iso.IntensityMostAbundantTheor = iso.Peaklist[indexMostAbundantPeakTheor].Height;
			}
			else
			{
				iso.IntensityMostAbundantTheor = iso.IntensityMostAbundant;
			}

			UpdateMonoisotopicMassData(iso);
		}

		private static void UpdateMonoisotopicMassData(IsotopicProfile iso)
		{
			iso.MonoIsotopicMass = (iso.getMonoPeak().XValue - Globals.PROTON_MASS) * iso.ChargeState;
			iso.MonoPeakMZ = iso.getMonoPeak().XValue;
			iso.MostAbundantIsotopeMass = (iso.getMostIntensePeak().XValue - Globals.PROTON_MASS) * iso.ChargeState;
		}

		public static double GetFit(float[] theorPeakList, double[] observedPeakList, int numPeaksToTheLeftForScoring = 0)
		{
			List<double> theorIntensitiesUsedInCalc = new List<double>();
			var observedIntensitiesUsedInCalc = new List<double>();

			//first gather all the intensities from theor and obs peaks
			double maxTheorIntensity = double.MinValue;
			for (int i = 0; i < theorPeakList.Length; i++)
			{
				if (theorPeakList[i] > maxTheorIntensity)
				{
					maxTheorIntensity = theorPeakList[i];
				}
			}

			for (int index = 0; index < theorPeakList.Length; index++)
			{
				double theoreticalPeak = theorPeakList[index];
				double observedPeak = observedPeakList[index];

				bool overrideMinIntensityCutoff = index < numPeaksToTheLeftForScoring;

				if (theoreticalPeak > 0.1 || overrideMinIntensityCutoff)
				{
					theorIntensitiesUsedInCalc.Add(theoreticalPeak);
					observedIntensitiesUsedInCalc.Add(observedPeak);
				}
			}

			//the minIntensityForScore is too high and no theor peaks qualified. This is bad. But we don't
			//want to throw errors here
			if (theorIntensitiesUsedInCalc.Count == 0)
			{
				return 1.0;
			}

			double maxObs = observedIntensitiesUsedInCalc.Max();
			if (Math.Abs(maxObs - 0) < float.Epsilon) maxObs = double.PositiveInfinity;

			List<double> normalizedObs = observedIntensitiesUsedInCalc.Select(p => p / maxObs).ToList();

			double maxTheor = theorIntensitiesUsedInCalc.Max();
			List<double> normalizedTheo = theorIntensitiesUsedInCalc.Select(p => p / maxTheor).ToList();

			double sumSquareOfDiffs = 0;
			double sumSquareOfTheor = 0;
			for (int i = 0; i < normalizedTheo.Count; i++)
			{
				var diff = normalizedObs[i] - normalizedTheo[i];

				sumSquareOfDiffs += (diff * diff);
				sumSquareOfTheor += (normalizedTheo[i] * normalizedTheo[i]);
			}

			double fitScore = sumSquareOfDiffs / sumSquareOfTheor;
			if (double.IsNaN(fitScore) || fitScore > 1) fitScore = 1;

			return fitScore;
		}
	}
}
