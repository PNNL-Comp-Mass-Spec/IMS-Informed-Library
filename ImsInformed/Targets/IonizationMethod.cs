﻿// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
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
namespace ImsInformed.Targets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Composition;

    /// <summary>
    /// The ionization method.
    /// </summary>
    public enum IonizationMethod
    {
        /// <summary>
        /// Ionization by adding a proton
        /// </summary>
        Protonated, // Add H+

        /// <summary>
        /// Ionization by removing a proton
        /// </summary>
        Deprotonated, // Take out H+

        /// <summary>
        /// Ionization by adding a sodium ion
        /// </summary>
        Sodiumated, // Add Na+

        /// <summary>
        /// Ionization by addving sodium ion and removing two protons
        /// </summary>
        Proton2MinusSodiumPlus, // Add Na+ and 2H-, net H- 

        /// <summary>
        /// Ionization with formic acid 
        /// </summary>
        HCOOMinus, // HCOO+

        /// <summary>
        /// Atmospheric pressure chemical ionization (APCI) is an ionization method used in mass spectrometry
        /// (commonly LC-MS) which utilizes gas-phase ion-molecule reactions at atmospheric pressure.
        /// </summary>
        APCI, 
    }

    /// <summary>
    /// The ionization method extensions.
    /// </summary>
    public static class IonizationMethodUtilities
    {
        /// <summary>
        /// The get charge state.
        /// </summary>
        /// <param name="ionizationMethod">
        /// The ionization method.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int GetChargeState(this IonizationMethod ionizationMethod)
        {
            if (ionizationMethod == IonizationMethod.APCI ||
                ionizationMethod == IonizationMethod.HCOOMinus || 
                ionizationMethod == IonizationMethod.Deprotonated || 
                ionizationMethod == IonizationMethod.Proton2MinusSodiumPlus)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// The get mass aduct sign.
        /// </summary>
        /// <param name="ionizationMethod">
        /// The ionization method.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int GetMassAductSign(this IonizationMethod ionizationMethod)
        {
            if (ionizationMethod == IonizationMethod.Deprotonated)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// The get all.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public static IEnumerable<IonizationMethod> GetAll()
        {
            return Enum.GetValues(typeof(IonizationMethod)).Cast<IonizationMethod>();
        }

        /// <summary>
        /// Get the 
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        public static Composition GetComposition(this IonizationMethod method)
        {
            // compensate for extra composition difference due to different ionization method
            if (method == IonizationMethod.Protonated)
            {
                return new Composition(0, 1, 0, 0, 0);
            }
            else if (method == IonizationMethod.Deprotonated) 
            {
                return new Composition(0, 1, 0, 0, 0);
            }
            else if (method == IonizationMethod.Sodiumated) 
            {
                return MoleculeUtil.ReadEmpiricalFormula("Na");
            }
            else if (method == IonizationMethod.APCI) 
            {
                return new Composition(0, 0, 0, 0, 0);
            }
            else if (method == IonizationMethod.HCOOMinus) 
            {
                return new Composition(1, 1, 0, 2, 0);
            }
            else if (method == IonizationMethod.Proton2MinusSodiumPlus)
            {
                Composition newCompo = MoleculeUtil.ReadEmpiricalFormula("Na");
                return newCompo - new Composition(0, 2, 0, 0, 0);
            }

            return null;
        }

        /// <summary>
        /// The to friendly string.
        /// </summary>
        /// <param name="ionizationMethod">
        /// The ionization method.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static string ToFriendlyString(this IonizationMethod ionizationMethod)
        {
            string method;

            if (ionizationMethod == IonizationMethod.Protonated)
            {
                method = "[M+H]";
            }
            else if (ionizationMethod == IonizationMethod.Deprotonated)
            {
                method = "[M-H]";
            }
            else if (ionizationMethod == IonizationMethod.Sodiumated)
            {
                method = "[M+Na]";
            }
            else if (ionizationMethod == IonizationMethod.APCI)
            {
                method = "[APCI]";
            }
            else if (ionizationMethod == IonizationMethod.HCOOMinus)
            {
                method = "[M+HCOO]";
            }
            else if (ionizationMethod == IonizationMethod.Proton2MinusSodiumPlus)
            {
                method = "[M-2H+Na]";
            }
            else 
            {
                throw new ArgumentException("Ionization method [" + ionizationMethod + "] is not supported");
            }

            return method;
        }

        /// <summary>
        /// Convert the ionization method to adduct, which is more generic.
        /// </summary>
        /// <param name="ionizationMethod">
        /// The ionization method.
        /// </param>
        /// <param name="multiplier">
        /// The multiplier.
        /// </param>
        /// <returns>
        /// The <see cref="IonizationAdduct"/>.
        /// </returns>
        public static IonizationAdduct ToAdduct(this IonizationMethod ionizationMethod, int multiplier = 1)
        {
            return new IonizationAdduct(ionizationMethod, multiplier);
        }

        /// <summary>
        /// The parse ionization method.
        /// </summary>
        /// <param name="ionizationMethod">
        /// user input for ionization method 
        /// </param>
        /// <returns>
        /// The <see cref="IonizationMethod"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static IonizationMethod ParseIonizationMethod(string ionizationMethod)
        {
            // get the ionization method.
            ionizationMethod = ionizationMethod.ToUpper();
            IonizationMethod method;
            if (ionizationMethod == "M+H")
            {
                method = IonizationMethod.Protonated;
            }
            else if (ionizationMethod == "M-H")
            {
                method = IonizationMethod.Deprotonated;
            }
            else if (ionizationMethod == "M+NA")
            {
                method = IonizationMethod.Sodiumated;
            }
            else if (ionizationMethod == "APCI")
            {
                method = IonizationMethod.APCI;
            }
            else if (ionizationMethod == "M+HCOO")
            {
                method = IonizationMethod.HCOOMinus;
            }
            else if (ionizationMethod == "M-2H+NA")
            {
                method = IonizationMethod.Proton2MinusSodiumPlus;
            }
            else 
            {
                throw new ArgumentException("Ionization method [" + ionizationMethod + "] is not recognized");
            }

            return method;
        }
    }
}
