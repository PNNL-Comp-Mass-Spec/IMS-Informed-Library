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
    using System.Collections.Generic;
    using System.IO;

    using MathNet.Numerics.Interpolation;

    /// <summary>
    /// The alignment importer.
    /// </summary>
    public class AlignmentImporter
    {
        /// <summary>
        /// The read file.
        /// </summary>
        /// <param name="fileLocation">
        /// The file location.
        /// </param>
        /// <returns>
        /// The <see cref="IInterpolation"/>.
        /// </returns>
        public static IInterpolation ReadFile(string fileLocation)
        {
            FileInfo fileInfo = new FileInfo(fileLocation);

            List<double> xValues = new List<double>();
            List<double> yValues = new List<double>();

            using (TextReader reader = new StreamReader(fileInfo.FullName))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    string[] splitLine = line.Split(',');

                    double xValue = double.Parse(splitLine[0]);
                    double yValue = double.Parse(splitLine[1]);

                    xValues.Add(xValue);
                    yValues.Add(yValue);
                }
            }

            IInterpolation interpolation = new StepInterpolation(xValues.ToArray(), yValues.ToArray());

            return interpolation;
        }
    }
}
