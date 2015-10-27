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

    using InformedProteomics.Backend.Data.Composition;

    /// <summary>
    /// The IMS Target interface.
    /// </summary>
    public interface IImsTarget : IEquatable<IImsTarget>
    {
        /// <summary>
        /// Gets the mass.
        /// </summary>
        double MonoisotopicMass { get; }

        /// <summary>
        /// Gets the typicalIonization type.
        /// </summary>
        IonizationAdduct Adduct { get; }

        /// <summary>
        /// Gets the Target type.
        /// </summary>
        TargetType TargetType { get; }

        /// <summary>
        /// Gets the empirical formula.
        /// </summary>
        string EmpiricalFormula { get; }

        /// <summary>
        /// Gets the composition.
        /// </summary>
        Composition CompositionWithoutAdduct { get; }

        /// <summary>
        /// Gets the composition.
        /// </summary>
        Composition CompositionWithAdduct { get; }

        /// <summary>
        /// Gets the target descriptor, human-readable information of what the target is about in terms of drift time, MZ, and so on.
        /// </summary>
        string TargetDescriptor { get; }

        /// <summary>
        /// Gets the chemical identifier, googleable, adduct independent information of what the chemical is.
        /// </summary>
        string CorrespondingChemical { get; }

        /// <summary>
        /// Gets the Target m/z.
        /// </summary>
        double MassWithAdduct { get; }

        /// <summary>
        /// Gets the Target m/z.
        /// </summary>
        int ChargeState { get; }

        /// <summary>
        /// Gets a value indicating whether has composition info.
        /// </summary>
        bool HasCompositionInfo { get; }
    }
}
