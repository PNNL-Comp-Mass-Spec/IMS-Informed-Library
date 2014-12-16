using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// utilities used to work on non-peptide small particles.
namespace ImsInformed.Util
{
    using System.Collections;
    using System.IO;
    using System.Net.Mime;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Input;
    using System.Xml.Linq;

    using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;

    using ImsInformed.Domain;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;

    using MultiDimensionalPeakFinding.PeakDetection;

    using UIMFLibrary;

    public static class MoleculeUtil
    {
        // Apply the ionization composition to the chemical of interest. Return null if the given composition is null.
        public static Composition IonizationCompositionCompensation(Composition composition, IonizationMethod method)
        {
            if (composition != null)
            {
                // compensate for extra composition difference due to different ionization method
                if (method == IonizationMethod.ProtonPlus)
				    return composition + new Composition(0, 1 , 0, 0, 0);
                else if (method == IonizationMethod.ProtonMinus)
                    return composition - new Composition(0, 1, 0, 0, 0);
                else if (method == IonizationMethod.SodiumPlus)
                    return composition + MoleculeUtil.ReadEmpiricalFormulaNoParenthesis("Na"); 
                else if (method == IonizationMethod.Proton2Plus)
                    return composition + new Composition(0, 2 , 0, 0, 0);
                else if (method == IonizationMethod.Proton2Minus)
                    return composition - new Composition(0, 2 , 0, 0, 0);
            }
            return null;
        }

        // Filter spurious data
        public static double NoiseClassifier(FeatureBlob bestFeature, double globalMaxIntensities)
        {
            double featureMaxIntensity = bestFeature.Statistics.IntensityMax;
            if (featureMaxIntensity * 10 >= globalMaxIntensities)
            {
                return 1;
            } 
            else if (featureMaxIntensity <= 0)
            {
                return 0;
            }
            else
            {
                return featureMaxIntensity*10 / globalMaxIntensities;
            }
        }

        // Calculate the max global intensities for the given voltage group.
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
                        sumIntensity += intensities[frame-1][scan-firstScan][bin-firstBin];
                    }
                    maxIntensities = (sumIntensity > maxIntensities) ? sumIntensity : maxIntensities;
                }
            }
            return maxIntensities;
        }

        // Since loading all the data onto the screen can overload the computer
        // get max global intensities locally then compare.
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

        public static double MaxGlobalIntensities2(VoltageGroup group, DataReader reader)
        {
            Stack<int[]> data = reader.GetFrameAndScanListByDescendingIntensity();
            return data.Pop()[0];
        }

        // return the maxium intensity value possible for a given voltage group
        // Note currently supports 8-bit digitizers, proceeds with caution when
        // dealing with 12-bit digitizers
        public static double MaxDigitization(VoltageGroup group, DataReader reader)
        {
            return 255 * group.AccumulationCount * reader.GetFrameParams(group.FirstFrameNumber).GetValueInt32(FrameParamKeyType.Accumulations);
        }


        public static Composition IonizationCompositionDecompensation(Composition composition, IonizationMethod method)
        {
            if (composition != null)
            {
                // decompensate for extra composition difference due to different ionization method
                if (method == IonizationMethod.ProtonPlus)
				    return composition - new Composition(0, 1 , 0, 0, 0);
                else if (method == IonizationMethod.ProtonMinus)
                    return composition + new Composition(0, 1, 0, 0, 0);
                else if (method == IonizationMethod.SodiumPlus)
                    return composition - MoleculeUtil.ReadEmpiricalFormulaNoParenthesis("Na"); 
                else if (method == IonizationMethod.Proton2Plus)
                    return composition - new Composition(0, 2 , 0, 0, 0);
                else if (method == IonizationMethod.Proton2Minus)
                    return composition + new Composition(0, 2 , 0, 0, 0);
            }
            return null;
        }

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

        // This method can parse empirical formulas with parenthethese
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

        // This method only parse empirical formulas without parenthethese
        // Examples, CHOCOOH, H2O, CO2, FeS
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
    }
}
