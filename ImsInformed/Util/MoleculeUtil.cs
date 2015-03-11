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
    using System.Xml.Linq;

    using ImsInformed.Domain;
    using ImsInformed.Stats;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    /// <summary>
    /// The molecule utilities.
    /// </summary>
    public static class MoleculeUtil
    {
        /// <summary>
        /// The ionization composition compensation.
        /// </summary>
        /// <param name="composition">
        /// The composition.
        /// </param>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        public static Composition IonizationCompositionCompensation(Composition composition, IonizationMethod method)
        {
            if (composition != null)
            {
                // compensate for extra composition difference due to different ionization method
                if (method == IonizationMethod.ProtonPlus)
                {
                    return composition + new Composition(0, 1, 0, 0, 0);
                }
                else if (method == IonizationMethod.ProtonMinus) 
                {
                    return composition - new Composition(0, 1, 0, 0, 0);
                }
                else if (method == IonizationMethod.SodiumPlus) 
                {
                    return composition + ReadEmpiricalFormulaNoParenthesis("Na");
                }
                else if (method == IonizationMethod.APCI) 
                {
                    return composition;
                }
                else if (method == IonizationMethod.HCOOMinus) 
                {
                    return composition + new Composition(1, 1, 0, 2, 0);
                }
                else if (method == IonizationMethod.Proton2MinusSodiumPlus)
                {
                    Composition newCompo = composition + ReadEmpiricalFormulaNoParenthesis("Na");
                    return newCompo - new Composition(0, 2, 0, 0, 0);
                }
            }

            return null;
        }

        /// <summary>
        /// The ionization composition decompensation.
        /// </summary>
        /// <param name="composition">
        /// The composition.
        /// </param>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The <see cref="Composition"/>.
        /// </returns>
        public static Composition IonizationCompositionDecompensation(Composition composition, IonizationMethod method)
        {
            if (composition != null)
            {
                // decompensate for extra composition difference due to different ionization method
                if (method == IonizationMethod.ProtonPlus)
                {
                    return composition - new Composition(0, 1, 0, 0, 0);
                }
                else if (method == IonizationMethod.ProtonMinus) 
                {
                    return composition + new Composition(0, 1, 0, 0, 0);
                }
                else if (method == IonizationMethod.SodiumPlus) 
                {
                    return composition - ReadEmpiricalFormulaNoParenthesis("Na");
                }
                else if (method == IonizationMethod.APCI) 
                {
                    return composition;
                }
                else if (method == IonizationMethod.HCOOMinus) 
                {
                    return composition - new Composition(1, 1, 0, 2, 0);
                }
                else if (method == IonizationMethod.Proton2MinusSodiumPlus)
                {
                    Composition newCompo = composition - ReadEmpiricalFormulaNoParenthesis("Na");
                    return newCompo + new Composition(0, 2, 0, 0, 0);
                }
            }
            return null;
        }

        /// <summary>
        /// The compute collision cross sectional area.
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
        /// The target MZ.
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
        /// Since loading all the data onto the screen can overload the computer
        /// get max global intensities locally then compare.
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
        [Obsolete("This method is really slow. Use MaxDigitization instead")]
        public static double MaxGlobalIntensities(VoltageGroup group, DataReader reader)
        {
            GlobalParams global = reader.GetGlobalParams();
            FrameParams param = reader.GetFrameParams(group.FirstFrameNumber);
            int firstFrame = group.FirstFrameNumber;
            int lastFrame = group.FirstFrameNumber + group.AccumulationCount;
            int firstScan = 1;
            int lastScan = param.Scans;
            int firstBin = 0;
            int lastBin = global.Bins;
            int windowOfBins = 1000;
            int windowOfScans = 20;
            double maxIntensities = 0;
            for (int scan = firstScan; scan <= lastScan; scan += windowOfScans)
            {
                int endScan = (scan + windowOfScans > lastScan) ? lastScan : scan + windowOfScans;
                for (int bin = firstBin; bin <= lastBin; bin += windowOfBins)
                {
                    int endBin = (bin + windowOfBins > lastBin) ? lastBin : bin + windowOfBins;
    
                    double localMaxIntensities = MaxGlobalIntensities(reader, scan, endScan, bin, endBin, firstFrame, lastFrame);
                    maxIntensities = (localMaxIntensities > maxIntensities) ? localMaxIntensities : maxIntensities;
                }
            }
            return maxIntensities;
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
        /// The normalize drift time.
        /// </summary>
        /// <param name="driftTime">
        /// The drift time.
        /// </param>
        /// <param name="group">
        /// The group.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double NormalizeDriftTime(double driftTime, VoltageGroup group)
        {
            double normalizedPressure = UnitConversion.Nondimensionalized2Torr(group.MeanPressureNondimensionalized) / UnitConversion.StandardImsPressureInTorr;
            double normalizedTemperature = UnitConversion.Nondimensionalized2Kelvin(group.MeanTemperatureNondimensionalized) / UnitConversion.RoomTemperatureInKelvin;
            return driftTime / normalizedPressure;
        }

        /// <summary>
        /// return the maximum intensity value possible for a given voltage group
        /// Note currently supports 8-bit digitizers, proceeds with caution when
        /// dealing with 12-bit digitizers
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
        public static double MaxDigitization(VoltageGroup group, DataReader reader)
        {
            return 255 * group.AccumulationCount * reader.GetFrameParams(group.FirstFrameNumber).GetValueInt32(FrameParamKeyType.Accumulations);
        }

        /// <summary>
        /// The read empirical formula.
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
            char[] leftParenthesisArray = {'(', '[', '<', '{'};
            char[] rightParenthesisArray = {')', ']', '>', '}'};
            HashSet<char> leftParenthesisSymbols = new HashSet<char>(leftParenthesisArray);
            HashSet<char> rightParenthesisSymbols = new HashSet<char>(rightParenthesisArray);
            int index = 0;
            int count = 0;

            // Check for parenthesis balance.
            while (index < empiricalFormula.Length)
            {
                if (leftParenthesisSymbols.Contains(empiricalFormula[index]))
                {
                    count++;
                } 
                else if (rightParenthesisSymbols.Contains(empiricalFormula[index]))
                {
                    count--;
                }
                
                if (count < 0)
                {
                    throw new ArgumentException("Extra right parenthesis: " + empiricalFormula, "empiricalFormula");
                }
                index++;
            }

            if (count > 0)
            {
                throw new ArgumentException("Extra left parenthesis: " + empiricalFormula, "empiricalFormula");
            }
            
            return ReadEmpiricalFormula(empiricalFormula, leftParenthesisSymbols, rightParenthesisSymbols);
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
        private static Composition ReadEmpiricalFormula(string empiricalFormula, HashSet<char> leftParenthesisSymbols, HashSet<char> rightParenthesisSymbols)
        {
            int index = 0; // reset index
            
            string beforeLeftParenthesisFormula = "";
            string afterRightParenthesisFormula = "";
            string afterLeftParenthesisFormula = "";

            // Locate left parenthesis.
            while (index < empiricalFormula.Length && !leftParenthesisSymbols.Contains(empiricalFormula[index]))
            {
                beforeLeftParenthesisFormula += empiricalFormula[index];
                index++;
            }

            if (index >= empiricalFormula.Length)
            {
                return ReadEmpiricalFormulaNoParenthesis(empiricalFormula);
            }

            if (index + 1 >= empiricalFormula.Length)
            {
                throw new ArgumentException("Parenthesis not properly closed: " + empiricalFormula, "empiricalFormula");
            }

            afterLeftParenthesisFormula = empiricalFormula.Substring(index + 1);
            
            Composition beforeParenthesis = ReadEmpiricalFormulaNoParenthesis(beforeLeftParenthesisFormula);
            Composition insideParenthesis = CloseParenthesis(afterLeftParenthesisFormula, out afterRightParenthesisFormula, leftParenthesisSymbols, rightParenthesisSymbols);
            Composition afterParenthesis = ReadEmpiricalFormula(afterRightParenthesisFormula);
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
        private static Composition CloseParenthesis(string stringToBeClosed, out string leftOver, HashSet<char> leftParenthesisSymbols, HashSet<char> rightParenthesisSymbols)
        {
            int index = 0; // reset index
            string insideParenthesisFormula = "";

            while (index < stringToBeClosed.Length && !rightParenthesisSymbols.Contains(stringToBeClosed[index]))
            {
                if (leftParenthesisSymbols.Contains(stringToBeClosed[index]))
                {
                    throw new NotImplementedException("Cannot parse strings");
                }
                insideParenthesisFormula += stringToBeClosed[index];
                index++;
            }

            if (index >= stringToBeClosed.Length)
            {
                throw new ArgumentException("Parenthesis not properly closed.");
            }

            if (index + 1 >= stringToBeClosed.Length)
            {
                leftOver = "";
            }

            int n;
            bool isNumeric = Char.IsNumber(stringToBeClosed[index + 1]);

            n = isNumeric ? stringToBeClosed[index + 1] - '0' : 1;

            Composition inside = ReadEmpiricalFormulaNoParenthesis(insideParenthesisFormula);

            Composition summedInside = new Composition(0,0,0,0,0);

            // multiply molecule the composition
            for (int i = 0; i < n; i++)
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
        public static Composition ReadEmpiricalFormulaNoParenthesis(string empiricalFormula)
        {
            int c = 0;
            int h = 0;
            int n = 0;
            int o = 0;
            int s = 0;
            int p = 0;
            Dictionary<string, int> dict = DeconTools.Backend.Utilities.EmpiricalFormulaUtilities.ParseEmpiricalFormulaString(empiricalFormula);
            
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
                string xml_str = Properties.Resources.PNNLOmicsElementData;
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
                        var elemen = xelement.Element("ElementIsotopes").Elements("Element");
                        IEnumerable<XElement> els = from el in elemen
                        where (string)(el.Element("Symbol")) == entry.Key select el;
                        string name = (string)els.Elements("Name").First().Value;
                        
                        if (!Double.TryParse(els.Elements("Isotope").First().Element("Mass").Value, out averageMass) ||
                            !int.TryParse(els.Elements("Isotope").First().Element("IsotopeNumber").Value, out norminalMass))
                        {
                            throw new Exception("XML file corrupted");
                        }
                        Atom atom = new Atom(entry.Key, averageMass, norminalMass, name);
                        additionalElements.Add(new Tuple<Atom, short>(atom, (short)entry.Value));
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
        /// Calculate the max global intensities for the given voltage group.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <param name="firstScan">
        /// The first scan.
        /// </param>
        /// <param name="lastScan">
        /// The last scan.
        /// </param>
        /// <param name="firstBin">
        /// The first bin.
        /// </param>
        /// <param name="lastBin">
        /// The last bin.
        /// </param>
        /// <param name="firstFrame">
        /// The first frame.
        /// </param>
        /// <param name="lastFrame">
        /// The last frame.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private static double MaxGlobalIntensities(DataReader reader, int firstScan, int lastScan, int firstBin, int lastBin, int firstFrame, int lastFrame)
        {
            int[][][] intensities = reader.GetIntensityBlock(firstFrame, lastFrame, DataReader.FrameType.MS1, firstScan, lastScan, firstBin, lastBin);
            long maxIntensities = 0;
            for (int scan = firstScan; scan <= lastScan; scan++)
            {
                for (int bin = firstBin; bin < lastBin; bin++)
                {
                    long sumIntensity = 0;
                    for (int frame = firstFrame; frame <= lastFrame; frame++)
                    {
                        sumIntensity += intensities[frame - 1][scan - firstScan][bin - firstBin];
                    }
                    maxIntensities = (sumIntensity > maxIntensities) ? sumIntensity : maxIntensities;
                }
            }
            return maxIntensities;
        }
    }
}
