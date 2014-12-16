using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImsInformed.Domain
{
    public enum IonizationMethod
    {
        ProtonPlus, // Add H+
        ProtonMinus, // Take out H+
        SodiumPlus, // Add Na+
        Proton2Plus, // Add 2 H+
        Proton2Minus // Take out 2 H+
    }
}
