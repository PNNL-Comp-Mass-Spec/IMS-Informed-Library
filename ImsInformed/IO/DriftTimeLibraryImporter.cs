// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DriftTimeLibraryImporter.cs" company="PNNL">
//   Written for the Department of Energy (PNNL, Richland, WA)
//   //   Copyright 2015, Battelle Memorial Institute.  All Rights Reserved.
// </copyright>
// <summary>
//   Defines the DriftTimeLibraryImporter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImsInformed.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;

    using DeconTools.Backend.Utilities.Converters;

    using ImsInformed.Domain;
    using ImsInformed.Targets;

    /// <summary>
    /// The drift time library importer.
    /// </summary>
    public class DriftTimeLibraryImporter
    {
        /// <summary>
        /// The import drift time library.
        /// </summary>
        /// <param name="inputPath">
        /// The input path.
        /// </param>
        /// <returns>
        /// The <see cref="IList"/>.
        /// </returns>
        public static IList<DriftTimeTarget> ImportDriftTimeLibrary(string inputPath)
        {
            if (!File.Exists(inputPath))
            {
                throw new Exception(string.Format("File: {0} not found", inputPath));
            }

            IList<DriftTimeTarget> targets = new List<DriftTimeTarget>();
            using (StreamReader libraryFile = new StreamReader(inputPath))
            {
                string line;
                int lineNumber = 0;
                while ((line = libraryFile.ReadLine()) != null)
                {
                    lineNumber++;

                    if (!(line.Trim().StartsWith("#") || string.IsNullOrWhiteSpace(line)))
                    {
                        string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        int size = parts.Count();
                        if (size != 6)
                        {
                            throw new Exception("Cannot parse line: " + line + " into a valid drift time target");
                        }
                        else
                        {
                            string targetName = parts[0];
                            string targetWithAdduct = parts[1];
                            double driftTime;
                            if (!double.TryParse(parts[4], out driftTime))
                            {
                                throw new FileFormatException("Cannot parse file info: " + parts[4] + " into a valid drift time");
                            }

                            int openIndex = targetWithAdduct.LastIndexOf('[');
                            int formulaLength = openIndex;
                            int closeIndex = targetWithAdduct.LastIndexOf(']');
                            int adductLength = closeIndex - openIndex - 1;
                            string formula = targetWithAdduct.Substring(0, formulaLength);
                            String adduct = targetWithAdduct.Substring(openIndex + 1, adductLength);
                             
                            DriftTimeTarget target = new DriftTimeTarget(targetName, driftTime, formula, IonizationMethodUtilities.ParseIonizationMethod(adduct));
                            targets.Add(target);
                        }
                    }
                }
            }

            return targets;
        }
    }
}
