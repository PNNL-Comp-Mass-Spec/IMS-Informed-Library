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
namespace ImsInformed.Domain.DataAssociation
{
    using System.IO;

    using QuickGraph.Graphviz;
    using QuickGraph.Graphviz.Dot;

    internal class ObservationTransitionGraphRenderingEngine : IDotEngine
    {
        public string Run(GraphvizImageType imageType, string dot, string outputFileName)
        {
            // using (FileStream stream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            // {
            //     StreamWriter writer = new StreamWriter(stream);
            //     writer.WriteLine(dot);
            // }

            string outputDir = "output";

            File.WriteAllText(Path.Combine(outputDir, outputFileName + ".txt"), dot);

            return outputFileName;
        }
    }
}
