// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsotopicProfileUtil.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the IsotopicProfileUtil type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Util
{
    using DeconTools.Backend;
    using DeconTools.Backend.Core;

    /// <summary>
    /// The isotopic profile util.
    /// </summary>
    internal class IsotopicProfileUtil
    {
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
    }
}
