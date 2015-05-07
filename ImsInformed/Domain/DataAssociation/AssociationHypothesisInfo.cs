// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssociationHypothesisInfo.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The analysis scores holder.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Domain.DataAssociation
{
    using System;

    using ImsInformed.Scoring;

    /// <summary>
    /// Information on merits of the optimal assiciation
    /// </summary>
    [Serializable]
    public class AssociationHypothesisInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssociationHypothesisInfo"/> class.
        /// </summary>
        /// <param name="probabilityOfDataGivenHypothesis">
        /// The probability of data given hypothesis.
        /// </param>
        /// <param name="probabilityOfHypothesisGivenData">
        /// The probability of hypothesis given data.
        /// </param>
        public AssociationHypothesisInfo(double probabilityOfDataGivenHypothesis, double probabilityOfHypothesisGivenData)
        {
            this.ProbabilityOfDataGivenHypothesis = probabilityOfDataGivenHypothesis;
            this.ProbabilityOfHypothesisGivenData = probabilityOfHypothesisGivenData;
        }

        /// <summary>
        /// The probability of data given hypothesis.
        /// </summary>
        public readonly double ProbabilityOfDataGivenHypothesis;

        /// <summary>
        /// The probability of hypothesis given data.
        /// </summary>
        public readonly double ProbabilityOfHypothesisGivenData;
    }
}
