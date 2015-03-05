namespace ImsInformed.Domain
{
    /// <summary>
    /// The ionization method.
    /// </summary>
    public enum IonizationMethod
    {
        ProtonPlus, // Add H+
        ProtonMinus, // Take out H+
        SodiumPlus, // Add Na+
        Proton2MinusSodiumPlus, // Add Na+ and 2H-, net H- 
        HCOOMinus, // HCOO+
        APCI, // e-
    }
}
