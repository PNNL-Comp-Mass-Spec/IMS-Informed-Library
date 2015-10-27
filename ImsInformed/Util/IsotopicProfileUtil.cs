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
