// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoleculeUtil.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   Copyright 2014, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   The molecule util.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using ImsInformed.Domain.DirectInjection;
    using ImsInformed.Properties;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

    using UIMFLibrary;

    /// <summary>
    /// The molecule utilities.
    /// </summary>
    internal static class MoleculeUtil
    {
        /// <summary>
        /// The open parenthesis symbols
        /// </summary>
        private static readonly string Open  = "([{<";

        /// <summary>
        /// The close parenthesis symbols
        /// </summary>
        private static readonly string Close = ")]}>";

        /// <summary>
        /// The math to compute collision cross sectional area.
        /// </summary>
        /// <param name="averageTemperatureInKelvin">
        /// The average temperature in kelvin.
        /// </param>
        /// <param name="mobility">
        /// The mobility.
        /// </param>
        /// <param name="chargeState">
        /// The charge state.
        /// </param>
        /// <param name="reducedMass">
        /// The reduced mass.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double ComputeCrossSectionalArea(double averageTemperatureInKelvin, double mobility, int chargeState, double reducedMass)
        {
            return 18459 / Math.Sqrt(reducedMass * averageTemperatureInKelvin) * chargeState / mobility;
        }

        /// <summary>
        /// The reduced mass.
        /// </summary>
        /// <param name="targetMz">
        /// The Target MZ.
        /// </param>
        /// <param name="bufferGas">
        /// The buffer gas composition. 
        /// Example: N2
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double ComputeReducedMass(double targetMz, Composition bufferGas)
        {
            double bufferGasMass = bufferGas.Mass;
            double result = (bufferGasMass * targetMz) / (bufferGasMass + targetMz);
            return result;
        }

        /// <summary>
        /// The max global intensities 2.
        /// </summary>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double MaxGlobalIntensities2(VoltageGroup group, DataReader reader)
        {
            Stack<int[]> data = reader.GetFrameAndScanListByDescendingIntensity();
            return data.Pop()[0];
        }

        /// <summary>
        /// Convert the empirical formula string to CompositionWithoutAdduct object defined Informed Proteomics
        /// </summary>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static Composition ReadEmpiricalFormula(string empiricalFormula)
        {
            empiricalFormula = empiricalFormula.Trim();

            if (!IsBalanced(empiricalFormula))
            {
                throw new ArgumentException("Parentheses are not balanced in [" + empiricalFormula + "].");
            }

            return ParseEmpiricalFormula(empiricalFormula);
        }

        /// <summary>
        /// The is balanced.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool IsBalanced(string input)
        {
            return IsBalanced(input, String.Empty);
        }

        /// <summary>
        /// Check the balance of parenthesis given a empirical formula
        /// </summary>
        /// <param name="input">
        /// The input string.
        /// </param>
        /// <param name="stack">
        /// The string stack to compare balance with.
        /// </param>
        /// <returns>
        /// true if the string has balanced string as the stack <see cref="bool"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        private static bool IsBalanced(string input, string stack)
        {
            return 
                String.IsNullOrEmpty(input) ? String.IsNullOrEmpty(stack) :
                IsOpen(input[0]) ? IsBalanced(input.Substring(1), input[0] + stack) :
                IsClose(input[0]) ? !String.IsNullOrEmpty(stack) && IsMatching(stack[0], input[0]) && IsBalanced(input.Substring(1), stack.Substring(1)) :
                IsBalanced(input.Substring(1), stack);
        }

        /// <summary>
        /// The character is an open parenthesis.
        /// </summary>
        /// <param name="ch">
        /// The character to be contested.
        /// </param>
        /// <returns>
        /// true if the character is an open parenthesis, false otherwise <see cref="bool"/>.
        /// </returns>
        private static bool IsOpen(char ch) 
        {
            return Open.IndexOf(ch) != -1;
        }

        /// <summary>
        /// The character is a closing parenthesis.
        /// </summary>
        /// <param name="ch">
        /// The character to be contested.
        /// </param>
        /// <returns>
        /// true if the character is an open parenthesis, false otherwise <see cref="bool"/>.
        /// </returns>
        private static bool IsClose(char ch) 
        {
            return Close.IndexOf(ch) != -1;
        }

        private static bool IsMatching(char open, char close)
        {
            return Open.IndexOf(open) == Close.IndexOf(close);
        }

        /// <summary>
        /// This method can parse empirical formulas with parentheses
        /// </summary>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <param name="leftParenthesisSymbols">
        /// The left parenthesis symbols.
        /// </param>
        /// <param name="rightParenthesisSymbols">
        /// The right parenthesis symbols.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        private static Composition ParseEmpiricalFormula(string empiricalFormula)
        {
            int index = 0; // reset index
            
            string beforeLeftParenthesisFormula = "";
            string afterRightParenthesisFormula = "";
            string afterLeftParenthesisFormula = "";

            // Locate left parenthesis.
            while (index < empiricalFormula.Length && !IsOpen(empiricalFormula[index]))
            {
                beforeLeftParenthesisFormula += empiricalFormula[index];
                index++;
            }

            if (index >= empiricalFormula.Length)
            {
                return ReadEmpiricalFormulaNoParenthesis(empiricalFormula);
            }

            afterLeftParenthesisFormula = empiricalFormula.Substring(index + 1);
            
            Composition beforeParenthesis = ReadEmpiricalFormulaNoParenthesis(beforeLeftParenthesisFormula);
            Composition insideParenthesis = CloseParenthesis(afterLeftParenthesisFormula, out afterRightParenthesisFormula);
            Composition afterParenthesis = ParseEmpiricalFormula(afterRightParenthesisFormula);
            return beforeParenthesis + insideParenthesis + afterParenthesis;
        }

        /// <summary>
        /// The close parenthesis.
        /// </summary>
        /// <param name="stringToBeClosed">
        /// The string to be closed.
        /// </param>
        /// <param name="leftOver">
        /// The left over.
        /// </param>
        /// <param name="leftParenthesisSymbols">
        /// The left parenthesis symbols.
        /// </param>
        /// <param name="rightParenthesisSymbols">
        /// The right parenthesis symbols.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        /// <exception cref="ArgumentException">
        /// </exception>
        private static Composition CloseParenthesis(string stringToBeClosed, out string leftOver)
        {
            int index = 0; // reset index
            string insideParenthesisFormula = "";
            bool isNumeric = false;

            int opennings = 1;
            while (index < stringToBeClosed.Length && opennings != 0)
            {
                if (IsClose(stringToBeClosed[index]))
                {
                    opennings--;
                }

                if (IsOpen(stringToBeClosed[index]))
                {
                    opennings++;
                }

                if (opennings != 0)
                {
                    insideParenthesisFormula += stringToBeClosed[index];
                    index++;
                }
            }

            index++;
            if (index < stringToBeClosed.Length)
            {
                isNumeric = Char.IsNumber(stringToBeClosed[index]);
            }

            int multiplier = isNumeric ? stringToBeClosed[index] - '0' : 1;

            Composition inside = ParseEmpiricalFormula(insideParenthesisFormula);

            Composition summedInside = new Composition(0,0,0,0,0);

            // multiply molecule the composition
            for (int i = 0; i < multiplier; i++)
            {
                summedInside += inside;
            }

            if (isNumeric)
            {
                index++;
            }

            leftOver = stringToBeClosed.Substring(index);

            return summedInside;
        }

        /// <summary>
        /// This method only parse empirical formulas without parenthethese
        /// Examples, CHOCOOH, H2O, CO2, FeS
        /// </summary>
        /// <param name="empiricalFormula">
        /// The empirical formula.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        private static Composition ReadEmpiricalFormulaNoParenthesis(string empiricalFormula)
        {
            int c = 0;
            int h = 0;
            int n = 0;
            int o = 0;
            int s = 0;
            int p = 0;
            IDictionary<string, int> dict = ParseEmpiricalFormulaString(empiricalFormula);
            
            c = (dict.ContainsKey("C")) ? dict["C"] : 0;
            h = (dict.ContainsKey("H")) ? dict["H"] : 0;
            n = (dict.ContainsKey("N")) ? dict["N"] : 0;
            o = (dict.ContainsKey("O")) ? dict["O"] : 0;
            s = (dict.ContainsKey("S")) ? dict["S"] : 0;
            p = (dict.ContainsKey("P")) ? dict["P"] : 0;

            dict.Remove("C");
            dict.Remove("H");
            dict.Remove("N");
            dict.Remove("O");
            dict.Remove("S");
            dict.Remove("P");

            if (dict.Keys.Count == 0)
                return new Composition(c, h, n, o, s, p);
            try
            {
                //load PNNLOmicsElementData.xml on runtime and consult it to construct the Atom profile
                string xml_str = Resources.PNNLOmicsElementData;
                XElement xelement = XElement.Parse(xml_str);

                // Convert the rest of the dictionary as an IEnumberable<tuple<string, int>>
                List<Tuple<Atom, short>> additionalElements = new List<Tuple<Atom, short>>();
                foreach (var entry in dict) 
                {
                    string symbol = entry.Key;
                    try
                    {
                        int norminalMass;
                        double averageMass;

                        // Find the element in the PNNL xml File.
                        var xElement = xelement.Element("ElementIsotopes");
                        if (xElement != null)
                        {
                            var elemen = xElement.Elements("Element");
                            IEnumerable<XElement> els = from el in elemen
                            where (string)(el.Element("Symbol")) == entry.Key select el;
                            string name = (string)els.Elements("Name").First().Value;
                        
                            if (!Double.TryParse(els.Elements("Isotope").First().Element("Mass").Value, out averageMass) ||
                                !Int32.TryParse(els.Elements("Isotope").First().Element("IsotopeNumber").Value, out norminalMass))
                            {
                                throw new Exception("XML file corrupted");
                            }
                            Atom atom = new Atom(entry.Key, averageMass, norminalMass, name);
                            additionalElements.Add(new Tuple<Atom, short>(atom, (short)entry.Value));
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Element not defined: " + e);
                    }
                }
                return new Composition(c, h, n, o, s, p, additionalElements);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to read atom info from PNNLOmicsElementData.xml. " + e);
            }
        }

        /// <summary>
        /// The parse empirical formula string.
        /// </summary>
        /// <param name="chemicalFormula">
        /// The chemical formula.
        /// </param>
        /// <returns>
        /// The <see cref="IDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <exception cref="FormatException">
        /// </exception>
        private static IDictionary<string, int> ParseEmpiricalFormulaString(string chemicalFormula)
        {
            IDictionary<string, int> formula = new Dictionary<string, int>();
            string elementRegex = "([A-Z][a-z]*)([0-9]*)";
            string validateRegex = "^(" + elementRegex + ")+$";

            if (String.IsNullOrEmpty(chemicalFormula))
            {
                return formula;
            }

            if (!Regex.IsMatch(chemicalFormula, validateRegex))
                throw new FormatException(String.Format("Cannot parse empirical formula for [{0}], please check format and case", chemicalFormula));

            foreach (Match match in Regex.Matches(chemicalFormula, elementRegex))
            {
                string name = match.Groups[1].Value;

                int count =
                    match.Groups[2].Value != "" ?
                    Int32.Parse(match.Groups[2].Value) :
                    1;

                if (formula.ContainsKey(name))
                {
                    formula[name] += count;
                }
                else
                {
                    formula.Add(name, count);
                }
            }

            return formula;
        }

        /// <summary>
        /// The are compositions equal.
        /// </summary>
        /// <param name="A">
        /// The a.
        /// </param>
        /// <param name="B">
        /// The b.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool AreCompositionsEqual(Composition A, Composition B)
        {
            Composition remainder;
            try
            {
                remainder = A - B;
            }
            catch (Exception)
            {
                remainder = B - A;                
            }

            return (remainder == null) || (remainder.Mass - 0) < 0.1;
        }
    }
}
