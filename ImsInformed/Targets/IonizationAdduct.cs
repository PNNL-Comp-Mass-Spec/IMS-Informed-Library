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
namespace ImsInformed.Targets
{
    using System;

    using ImsInformed.Domain;
    using ImsInformed.Util;

    using InformedProteomics.Backend.Data.Composition;

    /// <summary>
    /// The ion attached to the original chemical after the ionization process.
    /// </summary>
    [Serializable]
    public class IonizationAdduct : IEquatable<IonizationAdduct>
    {
        /// <summary>
        /// The composition.
        /// </summary>
        private readonly Composition compositionSurplus;

        /// <summary>
        /// The composition debt.
        /// </summary>
        private readonly Composition compositionDebt;

        /// <summary>
        /// The description.
        /// </summary>
        private readonly string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="IonizationAdduct"/> class.
        /// </summary>
        /// <param name="adductCompsition">
        /// The adduct Composition.
        /// </param>
        /// <param name="chargeState">
        /// The charge state.
        /// </param>
        /// <param name="compositionDebt">
        /// The composition debt.
        /// </param>
        public IonizationAdduct(Composition composition, int chargeState)
        {
            this.compositionSurplus = composition;
            this.ChargeState = chargeState;
            this.compositionDebt = new Composition(0, 0, 0, 0, 0);
            this.description += string.Format("[M+{0}]", composition.ToPlainString());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IonizationAdduct"/> class.
        /// </summary>
        /// <param name="numberOfProtons">
        /// The number of protons.
        /// </param>
        public IonizationAdduct(int numberOfProtons) : this(IonizationMethod.Protonated, numberOfProtons)
        {
            if (numberOfProtons <= 0)
            {
                throw new ArgumentException("number of protons needs to be greater than 0");
            }

            if (numberOfProtons > 1)
            {
                this.description = string.Format("[M+{0}H]", numberOfProtons);
            }
            else
            {
                this.description = "[M+H]";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IonizationAdduct"/> class.
        /// </summary>
        /// <param name="ionizationMethod">
        /// The ionization method.
        /// </param>
        /// <param name="multiplier">
        /// The multiplier.
        /// </param>
        public IonizationAdduct(IonizationMethod ionizationMethod, int multiplier = 1)
        {
            this.compositionSurplus = new Composition(0, 0, 0, 0, 0);
            this.compositionDebt = new Composition(0, 0, 0, 0, 0);

            if (multiplier < 0)
            {
                throw new ArgumentException("multiplier cannot be negative");
            }

            if (multiplier == 1)
            {
                this.description = ionizationMethod.ToFriendlyString();
            }
            else
            {
                this.description = string.Format("[({0}){1}]", ionizationMethod.ToFriendlyString(), multiplier);
            }

            for (int i = 0; i < multiplier; i++)
            {
                // compensate for extra composition difference due to different ionization method
                if (ionizationMethod == IonizationMethod.Protonated)
                {
                    this.compositionSurplus += new Composition(0, 1, 0, 0, 0);
                    this.ChargeState++;
                }
                else if (ionizationMethod == IonizationMethod.Deprotonated) 
                {
                    this.compositionDebt += new Composition(0, 1, 0, 0, 0);
                    this.ChargeState--;
                }
                else if (ionizationMethod == IonizationMethod.Sodiumated) 
                {
                    this.compositionSurplus += MoleculeUtil.ReadEmpiricalFormula("Na");
                    this.ChargeState++;
                }
                else if (ionizationMethod == IonizationMethod.APCI) 
                {
                    this.ChargeState--;
                }
                else if (ionizationMethod == IonizationMethod.HCOOMinus) 
                {
                    this.compositionSurplus += new Composition(1, 1, 0, 2, 0);
                    this.ChargeState--;
                }
                else if (ionizationMethod == IonizationMethod.Proton2MinusSodiumPlus)
                {
                    this.compositionSurplus += MoleculeUtil.ReadEmpiricalFormula("Na");
                    this.compositionDebt += new Composition(0, 2, 0, 0, 0);
                    this.ChargeState--;
                }
            }
        }

        /// <summary>
        /// Gets the charge state.
        /// </summary>
        public int ChargeState { get; private set; }

        /// <summary>
        /// The modify composition.
        /// </summary>
        /// <param name="chemicalToBeCharged">
        /// The chemical to be charged.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        public Composition ModifyComposition(Composition chemicalToBeCharged)
        {
            return chemicalToBeCharged + this.compositionSurplus - this.compositionDebt;
        }

        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool Equals(IonizationAdduct other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return MoleculeUtil.AreCompositionsEqual(this.compositionDebt, other.compositionDebt) && MoleculeUtil.AreCompositionsEqual(this.compositionDebt, other.compositionDebt) && this.ChargeState == other.ChargeState;
        }

        public override bool Equals(object other) 
        {
            return this.Equals(other as IonizationAdduct);
        }

        /// <summary>
        /// The get hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode() 
        {
            int result = 29;
            result = result * 13 + this.compositionDebt.ToPlainString().GetHashCode();
            result = result * 13 + this.compositionSurplus.ToPlainString().GetHashCode();
            result = result * 13 + this.ChargeState;
            return result;
        }

        public override string ToString()
        {
            return this.description;
        }
    }
}
