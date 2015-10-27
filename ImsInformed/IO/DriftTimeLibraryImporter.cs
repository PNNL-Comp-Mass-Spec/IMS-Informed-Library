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
namespace ImsInformed.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
                            throw new Exception("Cannot parse FitLine: " + line + " into a valid drift time Target");
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
