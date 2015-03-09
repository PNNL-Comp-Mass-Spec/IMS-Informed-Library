namespace ImsInformed.Domain
{
    using System;

    /// <summary>
    /// The ionization method.
    /// </summary>
    public enum IonizationMethod
    {
        /// <summary>
        /// Ionization by adding a proton
        /// </summary>
        ProtonPlus, // Add H+

        /// <summary>
        /// Ionization by removing a proton
        /// </summary>
        ProtonMinus, // Take out H+

        /// <summary>
        /// Ionization by adding a sodium ion
        /// </summary>
        SodiumPlus, // Add Na+

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
    public static class IonizationMethodExtensions
    {
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

            if (ionizationMethod == IonizationMethod.ProtonPlus)
            {
                method = "[M+H]";
            }
            else if (ionizationMethod == IonizationMethod.ProtonMinus)
            {
                method = "[M-H]";
            }
            else if (ionizationMethod == IonizationMethod.SodiumPlus)
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
        /// The parse ionization method.
        /// </summary>
        /// <param name="ionization">
        /// The ionization.
        /// </param>
        /// <param name="ionizationMethod"> user input for ionization method </param>
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
                method = IonizationMethod.ProtonPlus;
            }
            else if (ionizationMethod == "M-H")
            {
                method = IonizationMethod.ProtonMinus;
            }
            else if (ionizationMethod == "M+NA")
            {
                method = IonizationMethod.SodiumPlus;
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
